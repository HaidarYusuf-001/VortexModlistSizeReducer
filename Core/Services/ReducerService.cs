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
    private readonly IAnalyzerService _analyzerService;

    private readonly string[] _vortexPathExclusions = {
        "__vortex_staging_folder",
        "__folder_managed_by_vortex"
    };

    private readonly HashSet<string> _configExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".ini", ".json", ".txt", ".xml", ".yaml", ".yml", ".toml",
        ".cfg", ".conf", ".log", ".bat", ".cmd", ".ps1", ".sh",
        ".lua", ".psc", ".csv", ".tsv", ".md", ".msgpack", ".meta",
        ".dll"
    };

    public ReducerService(IHardlinkService hardlinkService, IAnalyzerService analyzerService)
    {
        _hardlinkService = hardlinkService;
        _analyzerService = analyzerService;
    }

    public void ExecuteReduction(string stagingPath)
    {
        if (!_analyzerService.ValidateDeploymentState(stagingPath))
        {
            Console.WriteLine("ABORTED: No deployed files detected. Ensure mods are deployed via Hardlink Deployment in Vortex.");
            return;
        }

        Console.WriteLine("INDEXING FILES. PLEASE WAIT...");
        var allFiles = Directory.GetFiles(stagingPath, "*.*", SearchOption.AllDirectories);

        long totalBytesSaved = 0;
        int filesDeleted = 0;
        int filesProcessed = 0;
        int totalFiles = allFiles.Length;

        ConcurrentQueue<string> deletedFilesLog = new();

        Console.WriteLine($"COMMENCING PARALLEL REDUCTION FOR {totalFiles} FILES...");

        Parallel.ForEach(allFiles, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, file =>
        {
            int currentProcessed = Interlocked.Increment(ref filesProcessed);

            if (currentProcessed % 10000 == 0)
            {
                Console.WriteLine($"PROCESSING: {currentProcessed} / {totalFiles} FILES ANALYZED...");
            }

            if (_vortexPathExclusions.Any(exclusion => file.Contains(exclusion, StringComparison.OrdinalIgnoreCase)))
            {
                return;
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