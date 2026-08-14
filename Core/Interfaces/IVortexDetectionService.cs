using System.Collections.Generic;

namespace VortexModlistReducer.Core.Interfaces;

public interface IVortexDetectionService
{
    string GetActiveStagingFolder();
    bool TryGetDisabledModFolders(out HashSet<string> disabledFolders);
}