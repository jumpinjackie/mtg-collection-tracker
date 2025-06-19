
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
        var fi = new FileInfo(file);
        if (fi.Exists)
            return fi.OpenRead();
        return null;
    }
}
