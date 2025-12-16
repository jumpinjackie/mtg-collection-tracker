using System.Collections.Concurrent;

namespace MtgCollectionTracker.Core.Services;

public class PriceCache
{
    private readonly ConcurrentDictionary<string, (DateTime Expiry, (string? Edition, decimal? Price) Meta)> _cache = new();

    public (string? Edition, decimal? Price)? Get(string cardName)
    {
        if (_cache.TryGetValue(cardName, out var cached) && cached.Expiry > DateTime.UtcNow)
        {
            return cached.Meta;
        }
        return null;
    }

    public void Set(string cardName, (string? Edition, decimal? Price) meta)
    {
        _cache[cardName] = (DateTime.UtcNow.AddHours(24), meta);
    }
}