using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using VortexModlistReducer.Core.Interfaces;

namespace VortexModlistReducer.Core.Services;

public class VortexDetectionService : IVortexDetectionService
{
    private string GetStateFilePath()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string vortexPath = Path.Combine(appData, "Vortex");

        string[] potentialPaths = {
            Path.Combine(vortexPath, "temp", "state_backups_full", "hourly.json")
        };

        foreach (var path in potentialPaths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        return string.Empty;
    }

    public string GetActiveStagingFolder()
    {
        string statePath = GetStateFilePath();

        if (string.IsNullOrEmpty(statePath))
        {
            throw new FileNotFoundException("Vortex state configuration and its backups could not be found.");
        }

        using var fileStream = new FileStream(statePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using JsonDocument doc = JsonDocument.Parse(fileStream);
        JsonElement root = doc.RootElement;

        string activeGameId = string.Empty;
        string activeProfileId = string.Empty;

        if (root.TryGetProperty("session", out JsonElement session))
        {
            if (session.TryGetProperty("activeGameId", out JsonElement gameIdElem))
                activeGameId = gameIdElem.GetString() ?? string.Empty;
            if (session.TryGetProperty("activeProfileId", out JsonElement profileIdElem))
                activeProfileId = profileIdElem.GetString() ?? string.Empty;
        }

        if (string.IsNullOrEmpty(activeGameId) || string.IsNullOrEmpty(activeProfileId))
        {
            if (root.TryGetProperty("settings", out JsonElement settings) &&
                settings.TryGetProperty("profiles", out JsonElement profilesSettings))
            {
                if (string.IsNullOrEmpty(activeProfileId) && profilesSettings.TryGetProperty("activeProfileId", out JsonElement apId))
                    activeProfileId = apId.GetString() ?? string.Empty;

                if (string.IsNullOrEmpty(activeGameId) && profilesSettings.TryGetProperty("lastActiveProfile", out JsonElement lastActive))
                {
                    foreach (var prop in lastActive.EnumerateObject())
                    {
                        if (prop.Value.GetString() == activeProfileId)
                        {
                            activeGameId = prop.Name;
                            break;
                        }
                    }
                }
            }
        }

        if (string.IsNullOrEmpty(activeGameId))
        {
            throw new InvalidOperationException("Failed to extract active game ID from Vortex state.");
        }

        string resultPath = string.Empty;

        if (root.TryGetProperty("settings", out JsonElement settingsMods) &&
            settingsMods.TryGetProperty("mods", out JsonElement modsNode))
        {
            if (modsNode.TryGetProperty("installPath", out JsonElement installPathNode) &&
                installPathNode.TryGetProperty(activeGameId, out JsonElement exactPathNode))
            {
                resultPath = exactPathNode.GetString() ?? string.Empty;
            }
            else if (modsNode.TryGetProperty("path", out JsonElement pathNode))
            {
                string basePath = pathNode.GetString() ?? string.Empty;
                if (!string.IsNullOrEmpty(basePath))
                {
                    resultPath = basePath.Contains("{game}", StringComparison.OrdinalIgnoreCase)
                        ? basePath.Replace("{game}", activeGameId, StringComparison.OrdinalIgnoreCase)
                        : Path.Combine(basePath, activeGameId);
                }
            }
        }

        if (string.IsNullOrEmpty(resultPath))
        {
            throw new InvalidOperationException("Failed to extract staging folder path from Vortex state.");
        }

        if (!Directory.Exists(resultPath))
        {
            throw new DirectoryNotFoundException($"Resolved staging path does not exist: {resultPath}");
        }

        return resultPath;
    }

    public bool TryGetDisabledModFolders(out HashSet<string> disabledFolders)
    {
        disabledFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            string statePath = GetStateFilePath();
            if (string.IsNullOrEmpty(statePath))
            {
                return false;
            }

            using var fileStream = new FileStream(statePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using JsonDocument doc = JsonDocument.Parse(fileStream);
            JsonElement root = doc.RootElement;

            string activeGameId = string.Empty;
            string activeProfileId = string.Empty;

            if (root.TryGetProperty("session", out JsonElement session))
            {
                if (session.TryGetProperty("activeGameId", out JsonElement gameIdElem))
                    activeGameId = gameIdElem.GetString() ?? string.Empty;
                if (session.TryGetProperty("activeProfileId", out JsonElement profileIdElem))
                    activeProfileId = profileIdElem.GetString() ?? string.Empty;
            }

            if (string.IsNullOrEmpty(activeGameId) || string.IsNullOrEmpty(activeProfileId))
            {
                if (root.TryGetProperty("settings", out JsonElement settings) &&
                    settings.TryGetProperty("profiles", out JsonElement profilesSettings))
                {
                    if (string.IsNullOrEmpty(activeProfileId) && profilesSettings.TryGetProperty("activeProfileId", out JsonElement apId))
                        activeProfileId = apId.GetString() ?? string.Empty;

                    if (string.IsNullOrEmpty(activeGameId) && profilesSettings.TryGetProperty("lastActiveProfile", out JsonElement lastActive))
                    {
                        foreach (var prop in lastActive.EnumerateObject())
                        {
                            if (prop.Value.GetString() == activeProfileId)
                            {
                                activeGameId = prop.Name;
                                break;
                            }
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(activeGameId) || string.IsNullOrEmpty(activeProfileId) || !root.TryGetProperty("persistent", out JsonElement persistent))
            {
                return false;
            }

            JsonElement modStates = default;
            if (persistent.TryGetProperty("profiles", out JsonElement profiles) &&
                profiles.TryGetProperty(activeProfileId, out JsonElement activeProfile) &&
                activeProfile.TryGetProperty("modState", out JsonElement ms))
            {
                modStates = ms;
            }

            if (persistent.TryGetProperty("mods", out JsonElement mods) &&
                mods.TryGetProperty(activeGameId, out JsonElement gameMods))
            {
                foreach (var modProp in gameMods.EnumerateObject())
                {
                    string modId = modProp.Name;
                    string installationPath = string.Empty;

                    if (modProp.Value.TryGetProperty("installationPath", out JsonElement rootPathElem))
                    {
                        installationPath = rootPathElem.GetString() ?? string.Empty;
                    }

                    if (string.IsNullOrEmpty(installationPath) && modProp.Value.TryGetProperty("attributes", out JsonElement attributes))
                    {
                        if (attributes.TryGetProperty("installationPath", out JsonElement pathElem))
                        {
                            installationPath = pathElem.GetString() ?? string.Empty;
                        }
                    }

                    if (string.IsNullOrEmpty(installationPath))
                    {
                        installationPath = modId;
                    }

                    bool isEnabled = false;
                    if (modStates.ValueKind != JsonValueKind.Undefined && modStates.TryGetProperty(modId, out JsonElement stateElement))
                    {
                        if (stateElement.TryGetProperty("enabled", out JsonElement enabledElement))
                        {
                            isEnabled = enabledElement.ValueKind == JsonValueKind.True;
                        }
                    }

                    if (!isEnabled && !string.IsNullOrEmpty(installationPath))
                    {
                        disabledFolders.Add(installationPath);
                    }
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}