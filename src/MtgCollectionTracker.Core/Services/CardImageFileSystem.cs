
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
        // Read all bytes into a MemoryStream so the file handle is released immediately,
        // avoiding file locking and contention when multiple callers access the same image.
        return new MemoryStream(File.ReadAllBytes(file));
    }
}
