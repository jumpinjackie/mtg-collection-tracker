
namespace MtgCollectionTracker.Core.Services;

public class CardImageFileSystem(string baseDir) : ICardImageFileSystem
{
    public Stream OpenStream(string scryfallId, string tag)
    {
        var file = Path.Combine(baseDir, $"{scryfallId}_{tag}.jpg");
        return File.Open(file, FileMode.OpenOrCreate);
    }

    public Stream? TryGetStream(string scryfallId, string tag)
    {
        var file = Path.Combine(baseDir, $"{scryfallId}_{tag}.jpg");
        if (!File.Exists(file))
            return null;
        // Copy into a recyclable memory stream so the file handle is released immediately
        // without churning fresh MemoryStream allocations for every image read.
        using var fileStream = File.OpenRead(file);
        var stream = MemoryStreamPool.GetStream(nameof(CardImageFileSystem));
        fileStream.CopyTo(stream);
        stream.Position = 0;
        return stream;
    }
}
