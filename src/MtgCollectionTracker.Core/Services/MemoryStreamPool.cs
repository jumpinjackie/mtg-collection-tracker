using Microsoft.IO;

namespace MtgCollectionTracker.Core.Services;

public class MemoryStreamPool
{
    static readonly RecyclableMemoryStreamManager smManager = new RecyclableMemoryStreamManager();

    public static MemoryStream GetStream() => smManager.GetStream();

    public static MemoryStream GetStream(string? tag) => smManager.GetStream(tag);

    public static MemoryStream GetStream(byte[] buffer) => smManager.GetStream(buffer);

    public static MemoryStream GetStream(string? tag, byte[] buffer) => smManager.GetStream(tag, buffer);
}

