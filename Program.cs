using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VortexModlistReducer.Core.Interfaces;
using VortexModlistReducer.Core.Services;

namespace VortexModlistReducer;

class Program
{
    [DllImport("kernel32.dll")]
    static extern IntPtr GetConsoleWindow();

    static void Main(string[] args)
    {
        if (GetConsoleWindow() == IntPtr.Zero)
        {
            string safeArgs = string.Join(" ", args.Select(a => $"\"{a}\""));

            var processStartInfo = new ProcessStartInfo
            {
                FileName = Environment.ProcessPath,
                UseShellExecute = true,
                Arguments = safeArgs
            };

            Process.Start(processStartInfo);

            return;
        }

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<IHardlinkService, HardlinkService>();
                services.AddSingleton<IReducerService, ReducerService>();
                services.AddSingleton<IVortexDetectionService, VortexDetectionService>();
            })
            .Build();

        string stagingFolder = string.Empty;

        if (args.Length > 0)
        {
            stagingFolder = args[0];
        }
        else
        {
            Console.WriteLine("NO COMMAND LINE ARGUMENTS DETECTED.");
            Console.WriteLine("ATTEMPTING TO AUTO-DETECT ACTIVE VORTEX STAGING FOLDER...\n");

            try
            {
                var detectionService = host.Services.GetRequiredService<IVortexDetectionService>();
                stagingFolder = detectionService.GetActiveStagingFolder();
                Console.WriteLine($"AUTO-DETECTION SUCCESSFUL: {stagingFolder}\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AUTO-DETECTION FAILED: {ex.Message}");
                Console.WriteLine("Usage: VortexModlistReducer <Path_To_Vortex_Staging_Folder>");
                Console.WriteLine("\nPRESS ENTER TO EXIT...");
                Console.ReadLine();
                return;
            }
        }

        var reducerService = host.Services.GetRequiredService<IReducerService>();

        try
        {
            reducerService.ExecuteReduction(stagingFolder);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nCRITICAL ERROR: {ex.Message}");
        }

        Console.WriteLine("\nPRESS ENTER TO EXIT...");
        Console.ReadLine();
    }
}