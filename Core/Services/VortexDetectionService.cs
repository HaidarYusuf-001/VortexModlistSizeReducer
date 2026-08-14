using System;
using System.IO;
using System.Text.Json;
using VortexModlistReducer.Core.Interfaces;

namespace VortexModlistReducer.Core.Services;

public class VortexDetectionService : IVortexDetectionService
{
    public string GetActiveStagingFolder()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string statePath = Path.Combine(appData, "Vortex", "state.json");

        if (!File.Exists(statePath))
        {
            throw new FileNotFoundException("Vortex state.json not found.");
        }

        using var fileStream = new FileStream(statePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using JsonDocument doc = JsonDocument.Parse(fileStream);
        JsonElement root = doc.RootElement;

        string activeGameId = root.GetProperty("session").GetProperty("activeGameId").GetString() ?? string.Empty;
        string basePath = root.GetProperty("settings").GetProperty("mods").GetProperty("path").GetString() ?? string.Empty;

        if (string.IsNullOrEmpty(activeGameId) || string.IsNullOrEmpty(basePath))
        {
            throw new InvalidOperationException("Failed to extract configuration from state.json.");
        }

        string resultPath = basePath.Contains("{game}", StringComparison.OrdinalIgnoreCase)
            ? basePath.Replace("{game}", activeGameId, StringComparison.OrdinalIgnoreCase)
            : Path.Combine(basePath, activeGameId);

        if (!Directory.Exists(resultPath))
        {
            throw new DirectoryNotFoundException($"Resolved staging path does not exist: {resultPath}");
        }

        return resultPath;
    }
}