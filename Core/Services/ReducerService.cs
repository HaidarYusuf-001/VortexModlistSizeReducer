using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VortexModlistReducer.Core.Interfaces;

namespace VortexModlistReducer.Core.Services;

public class ReducerService : IReducerService
{
    private readonly IHardlinkService _hardlinkService;
    private readonly IVortexDetectionService _vortexDetectionService;

    private readonly string[] _vortexPathExclusions = {
        "__vortex_staging_folder",
        "__folder_managed_by_vortex"
    };

    private readonly HashSet<string> _configExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".ini", ".json", ".txt", ".xml", ".yaml", ".yml", ".toml",
        ".cfg", ".conf", ".log", ".bat", ".cmd", ".ps1", ".sh",
        ".lua", ".psc", ".csv", ".tsv", ".md", ".msgpack", ".meta",
        ".dll", ".swf", ".esp"
    };

    public ReducerService(IHardlinkService hardlinkService, IVortexDetectionService vortexDetectionService)
    {
        _hardlinkService = hardlinkService;
        _vortexDetectionService = vortexDetectionService;
    }

    public void ExecuteReduction(string stagingPath)
    {
        Console.WriteLine("SCANNING MOD DIRECTORIES...");
        var modDirectories = Directory.GetDirectories(stagingPath);

        bool canDetectDisabled = _vortexDetectionService.TryGetDisabledModFolders(out var knownDisabledFolders);

        var activeMods = new ConcurrentBag<string>();
        var overwrittenMods = new ConcurrentBag<string>();
        var disabledMods = new ConcurrentBag<string>();

        Parallel.ForEach(modDirectories, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, dir =>
        {
            if (_vortexPathExclusions.Any(exclusion => dir.Contains(exclusion, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            bool isActive = false;
            try
            {
                var files = Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    if (_hardlinkService.GetLinkCount(file) > 1)
                    {
                        isActive = true;
                        break;
                    }
                }
            }
            catch (Exception)
            {
            }

            if (isActive)
            {
                activeMods.Add(dir);
            }
            else
            {
                string dirName = new DirectoryInfo(dir).Name;
                if (canDetectDisabled)
                {
                    if (knownDisabledFolders.Contains(dirName))
                    {
                        disabledMods.Add(dir);
                    }
                    else
                    {
                        overwrittenMods.Add(dir);
                    }
                }
                else
                {
                    disabledMods.Add(dir);
                }
            }
        });

        if (!activeMods.Any())
        {
            Console.WriteLine("ABORTED: No deployed mods detected. Ensure mods are deployed via Hardlink Deployment in Vortex.");
            return;
        }

        Console.WriteLine($"\nANALYSIS COMPLETE:");
        Console.WriteLine($"- Active Mods Detected: {activeMods.Count}");

        if (canDetectDisabled)
        {
            Console.WriteLine($"- 100% Overwritten Mods Detected (Will be processed): {overwrittenMods.Count}");
            Console.WriteLine($"- Truly Disabled Mods Detected: {disabledMods.Count}");
        }
        else
        {
            Console.WriteLine($"- Disabled / 100% Overwritten Mods Detected: {disabledMods.Count}");
        }

        bool processDisabled = false;
        if (disabledMods.Any())
        {
            Console.WriteLine("\nWARNING: Processing disabled mods will COMPLETELY UNINSTALL/DELETE their files.");
            Console.Write("Do you want to delete files from Truly Disabled Mods? (Y/N): ");
            var input = Console.ReadLine();
            processDisabled = input?.Trim().Equals("Y", StringComparison.OrdinalIgnoreCase) ?? false;
        }

        var targetDirectories = activeMods.ToList();
        targetDirectories.AddRange(overwrittenMods);

        if (processDisabled)
        {
            targetDirectories.AddRange(disabledMods);
        }

        Console.WriteLine("\nINDEXING TARGET FILES. PLEASE WAIT...");

        var allFiles = new ConcurrentBag<string>();
        Parallel.ForEach(targetDirectories, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, dir =>
        {
            try
            {
                foreach (var file in Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories))
                {
                    allFiles.Add(file);
                }
            }
            catch (Exception)
            {
            }
        });

        int totalFiles = allFiles.Count;
        long totalBytesSaved = 0;
        int filesDeleted = 0;
        int filesProcessed = 0;

        ConcurrentQueue<string> deletedFilesLog = new();

        Console.WriteLine($"COMMENCING PARALLEL REDUCTION FOR {totalFiles} FILES...");

        Parallel.ForEach(allFiles, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, file =>
        {
            int currentProcessed = Interlocked.Increment(ref filesProcessed);

            if (currentProcessed % 10000 == 0)
            {
                Console.WriteLine($"PROCESSING: {currentProcessed} / {totalFiles} FILES ANALYZED...");
            }

            string extension = Path.GetExtension(file);
            if (_configExtensions.Contains(extension))
            {
                return;
            }

            try
            {
                var linkCount = _hardlinkService.GetLinkCount(file);

                if (linkCount == 1)
                {
                    var fileInfo = new FileInfo(file);
                    long length = fileInfo.Length;

                    fileInfo.IsReadOnly = false;
                    fileInfo.Delete();

                    Interlocked.Add(ref totalBytesSaved, length);
                    Interlocked.Increment(ref filesDeleted);

                    deletedFilesLog.Enqueue(file);
                }
            }
            catch (Exception)
            {
            }
        });

        double savedMB = totalBytesSaved / 1048576.0;
        double savedGB = totalBytesSaved / 1073741824.0;

        Console.WriteLine("\nREDUCTION COMPLETE.");
        Console.WriteLine($"Files Processed: {filesProcessed}");
        Console.WriteLine($"Files Deleted: {filesDeleted}");
        Console.WriteLine($"Space Saved: {savedMB:F2} MB ({savedGB:F2} GB)");

        if (!deletedFilesLog.IsEmpty)
        {
            try
            {
                string logFileName = $"DeletedFilesLog_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, logFileName);
                File.WriteAllLines(logPath, deletedFilesLog);
                Console.WriteLine($"\nVERBOSE LOG SAVED TO: {logPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nFAILED TO WRITE LOG FILE: {ex.Message}");
            }
        }
    }
}