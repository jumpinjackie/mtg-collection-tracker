namespace MtgCollectionTracker.Core.Services;

public interface ICardImageFileSystem
{
    Stream OpenStream(string scryfallId, string tag);

    Stream? TryGetStream(string scryfallId, string tag);
}
