using System;
using System.IO;
using VortexModlistReducer.Core.Interfaces;
using VortexModlistReducer.Native;

namespace VortexModlistReducer.Core.Services;

public class HardlinkService : IHardlinkService
{
    public uint GetLinkCount(string filePath)
    {
        using var handle = NativeInterop.CreateFile(
            filePath,
            0,
            NativeInterop.FILE_SHARE_READ | NativeInterop.FILE_SHARE_WRITE,
            IntPtr.Zero,
            NativeInterop.OPEN_EXISTING,
            NativeInterop.FILE_FLAG_BACKUP_SEMANTICS,
            IntPtr.Zero);

        if (handle.IsInvalid)
        {
            throw new IOException(filePath);
        }

        if (!NativeInterop.GetFileInformationByHandle(handle, out BY_HANDLE_FILE_INFORMATION fileInfo))
        {
            throw new IOException(filePath);
        }

        return fileInfo.NumberOfLinks;
    }
}