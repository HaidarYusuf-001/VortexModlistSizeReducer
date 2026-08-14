namespace VortexModlistReducer.Core.Interfaces;

public interface IHardlinkService
{
    uint GetLinkCount(string filePath);
}