using Microsoft.EntityFrameworkCore;
using MtgCollectionTracker.Data;
using ScryfallApi.Client;

namespace MtgCollectionTracker.Core.Services;

internal class ScryfallMetadataResolver
{
    record struct ScryfallMetaIdentity(string cardName, string edition, string language, string? collectorNumber);

    readonly Dictionary<ScryfallMetaIdentity, ScryfallCardMetadata> _dict = new();

    readonly CardsDbContext _db;
    readonly IScryfallApiClient? _scryfallApiClient;
    static readonly HttpClient _http = new();

    public ScryfallMetadataResolver(CardsDbContext db, IScryfallApiClient? scryfallApiClient)
    {
        _db = db;
        _scryfallApiClient = scryfallApiClient;
    }

    public int CacheHits { get; private set; } = 0;

    public int ScryfallApiCalls { get; private set; } = 0;

    public int ScryfallLargeImageFetches { get; private set; } = 0;

    public int ScryfallSmallImageFetches { get; private set; } = 0;

    static string? NullIf(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;

    public async ValueTask<ScryfallCardMetadata?> TryResolveAsync(string cardName,
                                                                  string edition,
                                                                  string? language,
                                                                  string? collectorNumber,
                                                                  bool refetchMetadata,
                                                                  CancellationToken cancel)
    {
        var nCardName = cardName.ToLower();
        var nEdition = edition.ToLower();
        var nLanguage = (NullIf(language) ?? "en").ToLower();
        var nCollectorNum = collectorNumber?.ToLower();

        var key = new ScryfallMetaIdentity(nCardName, nEdition, nLanguage, nCollectorNum);
        if (_dict.TryGetValue(key, out var sfMeta))
        {
            this.CacheHits++;
            return sfMeta;
        }
        else
        {
            // Try db lookup. Any metadata entries we save will be saved with normalised properties, so find
            // using normalised values
            sfMeta = await _db.Set<ScryfallCardMetadata>().FirstOrDefaultAsync(m => m.CardName == key.cardName
                && m.Edition == key.edition
                && m.Language == key.language
                && m.CollectorNumber == key.collectorNumber, cancel);

            if (sfMeta != null)
            {
                _dict.Add(key, sfMeta);
            }
        }

        if ((refetchMetadata || sfMeta == null) && _scryfallApiClient != null)
        {
            ScryfallApi.Client.Models.Card? sfCardMeta = null;
            try
            {
                // Resolve scryfall metadata
                var sfCards = await _scryfallApiClient.Cards.Search(key.cardName, 1, new ScryfallApi.Client.Models.SearchOptions()
                {
                    IncludeMultilingual = true,
                    Mode = ScryfallApi.Client.Models.SearchOptions.RollupMode.Prints
                });
                this.ScryfallApiCalls++;
                sfCardMeta = sfCards.Data.FirstOrDefault(c => string.Equals(c.Name, key.cardName, StringComparison.OrdinalIgnoreCase) && string.Equals(c.Set, key.edition, StringComparison.OrdinalIgnoreCase) && string.Equals(c.Language, key.language, StringComparison.OrdinalIgnoreCase) && string.Equals(c.CollectorNumber, key.collectorNumber, StringComparison.OrdinalIgnoreCase))
                          ?? sfCards.Data.FirstOrDefault(c => string.Equals(c.Name, key.cardName, StringComparison.OrdinalIgnoreCase) && string.Equals(c.Set, key.edition, StringComparison.OrdinalIgnoreCase) && string.Equals(c.Language, key.language, StringComparison.OrdinalIgnoreCase))
                          ?? sfCards.Data.FirstOrDefault(c => string.Equals(c.Name, key.cardName, StringComparison.OrdinalIgnoreCase) && string.Equals(c.Set, key.edition, StringComparison.OrdinalIgnoreCase))
                          ?? sfCards.Data.FirstOrDefault(c => string.Equals(c.Name, key.cardName, StringComparison.OrdinalIgnoreCase));
            }
            catch { }
            if (sfCardMeta != null)
            {
                if (sfMeta == null)
                {
                    // Before we make this entity, just check that we don't already have an existing metadata entry
                    // by this scryfall id
                    sfMeta = await _db.Set<ScryfallCardMetadata>().FindAsync(sfCardMeta.Id.ToString(), cancel);
                }
                // Now we create and add this entity if not foun
                if (sfMeta == null)
                {
                    sfMeta = new ScryfallCardMetadata
                    {
                        Id = sfCardMeta.Id.ToString(),
                        CardName = key.cardName,
                        Edition = key.edition,
                        CardType = sfCardMeta.TypeLine,
                        Rarity = sfCardMeta.Rarity

                    };
                    await _db.Set<ScryfallCardMetadata>().AddAsync(sfMeta, cancel);
                }
                
                // New or existing, update these properties if different

                // Collector #
                if (sfMeta.CollectorNumber != sfCardMeta.CollectorNumber)
                    sfMeta.CollectorNumber = sfCardMeta.CollectorNumber;

                // Large card image
                if (sfMeta.ImageLarge == null)
                {
                    byte[]? imageLarge = null;
                    if (sfCardMeta.ImageUris.TryGetValue("large", out var largeUri))
                    {
                        try
                        {
                            imageLarge = await _http.GetByteArrayAsync(largeUri, cancel);
                            sfMeta.ImageLarge = imageLarge;
                            this.ScryfallLargeImageFetches++;
                        }
                        catch { }
                    }
                }

                // Small card image
                if (sfMeta.ImageSmall == null)
                {
                    byte[]? imageSmall = null;
                    if (sfCardMeta.ImageUris.TryGetValue("small", out var smallUri))
                    {
                        try
                        {
                            imageSmall = await _http.GetByteArrayAsync(smallUri, cancel);
                            sfMeta.ImageSmall = imageSmall;
                            this.ScryfallSmallImageFetches++;
                        }
                        catch { }
                    }
                }

                _dict.Add(key, sfMeta);
            }
        }

        return sfMeta;
    }
}
