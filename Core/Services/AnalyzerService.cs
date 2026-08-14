using System.IO;
using System.Linq;
using VortexModlistReducer.Core.Interfaces;

namespace VortexModlistReducer.Core.Services;

public class AnalyzerService : IAnalyzerService
{
    private readonly IHardlinkService _hardlinkService;

    public AnalyzerService(IHardlinkService hardlinkService)
    {
        _hardlinkService = hardlinkService;
    }

    public bool ValidateDeploymentState(string stagingPath)
    {
        var sampleFiles = Directory.EnumerateFiles(stagingPath, "*.*", SearchOption.AllDirectories)
            .Take(1000)
            .ToList();

        if (!sampleFiles.Any())
        {
            return false;
        }

        int deployedCount = 0;

        foreach (var file in sampleFiles)
        {
            var linkCount = _hardlinkService.GetLinkCount(file);
            if (linkCount > 1)
            {
                deployedCount++;
            }
        }

        return deployedCount > 0;
    }
}