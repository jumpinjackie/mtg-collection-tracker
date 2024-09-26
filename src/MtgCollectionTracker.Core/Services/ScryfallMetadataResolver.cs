using Microsoft.EntityFrameworkCore;
using MtgCollectionTracker.Data;
using ScryfallApi.Client;
using ScryfallApi.Client.Models;

namespace MtgCollectionTracker.Core.Services;

public record ScryfallResolvedCard(string CardName, string Edition);

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

    public int ScryfallSmallImageFetches { get; private set; } = 0;

    static string? NullIf(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;

    public async ValueTask<(ScryfallResolvedCard? meta, bool added)> ResolveEditionAsync(string cardName, CancellationToken cancel)
    {
        if (_scryfallApiClient == null)
            return (null, false);

        // Resolve scryfall metadata
        var sfCards = await _scryfallApiClient.Cards.Search(cardName, 1, new SearchOptions()
        {
            IncludeMultilingual = true,
            Mode = SearchOptions.RollupMode.Prints
        });
        this.ScryfallApiCalls++;

        if (sfCards.Data.Count > 0)
        {
            var sfc = sfCards.Data.First();

            var res = new ScryfallResolvedCard(sfc.Name, sfc.Set.ToUpper());
            var (_, added) = await TryAddMetadataAsync(res.CardName, res.Edition, sfc, cancel);
            return (res, added);
        }
        return (null, false);
    }

    private async ValueTask<Card?> FindCardAsync(ScryfallMetaIdentity key)
    {
        Card? sfCardMeta = null;
        if (_scryfallApiClient == null)
            return sfCardMeta;

        var cards = new List<Card>();

        // Resolve scryfall metadata
        int pageNo = 0;
        while (true)
        {
            pageNo++;
            try
            {
                var sfCards = await _scryfallApiClient.Cards.Search(key.cardName, pageNo, new SearchOptions()
                {
                    IncludeMultilingual = true,
                    Mode = SearchOptions.RollupMode.Prints
                });
                this.ScryfallApiCalls++;
                cards.AddRange(sfCards.Data.Where(c => string.Equals(c.Name, key.cardName, StringComparison.OrdinalIgnoreCase)));
                if (!sfCards.HasMore)
                    break;
            }
            catch (ScryfallApiException se)
            {
                break;
            }
        }

        // In the event of in-exact match, prefer oldest printing
        cards.Sort((a, b) => a.ReleasedAt.CompareTo(b.ReleasedAt));

        // If proxy, prefer oldest printing of given language before falling back to english
        if (string.Equals(key.edition, "proxy", StringComparison.OrdinalIgnoreCase))
        {
            sfCardMeta = cards.FirstOrDefault(c => string.Equals(c.Name, key.cardName, StringComparison.OrdinalIgnoreCase) && string.Equals(c.Language, key.language, StringComparison.OrdinalIgnoreCase))
                ?? cards.FirstOrDefault(c => string.Equals(c.Name, key.cardName, StringComparison.OrdinalIgnoreCase) && string.Equals(c.Language, "en", StringComparison.OrdinalIgnoreCase))
                ?? cards.FirstOrDefault(c => string.Equals(c.Name, key.cardName, StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            sfCardMeta = cards.FirstOrDefault(c => string.Equals(c.Name, key.cardName, StringComparison.OrdinalIgnoreCase) && string.Equals(c.Set, key.edition, StringComparison.OrdinalIgnoreCase) && string.Equals(c.Language, key.language, StringComparison.OrdinalIgnoreCase) && string.Equals(c.CollectorNumber, key.collectorNumber, StringComparison.OrdinalIgnoreCase))
                    ?? cards.FirstOrDefault(c => string.Equals(c.Name, key.cardName, StringComparison.OrdinalIgnoreCase) && string.Equals(c.Set, key.edition, StringComparison.OrdinalIgnoreCase) && string.Equals(c.Language, key.language, StringComparison.OrdinalIgnoreCase))
                    ?? cards.FirstOrDefault(c => string.Equals(c.Name, key.cardName, StringComparison.OrdinalIgnoreCase) && string.Equals(c.Set, key.edition, StringComparison.OrdinalIgnoreCase))
                    ?? cards.FirstOrDefault(c => string.Equals(c.Name, key.cardName, StringComparison.OrdinalIgnoreCase));
        }

        return sfCardMeta;
    }

    private async ValueTask<(ScryfallCardMetadata meta, bool added)> TryAddMetadataAsync(string name, string edition, Card sfCardMeta, CancellationToken cancel)
    {
        // Before we make this entity, just check that we don't already have an existing metadata entry
        // by this scryfall id
        var sfMeta = await _db.Set<ScryfallCardMetadata>().FindAsync(sfCardMeta.Id.ToString(), cancel);
        if (sfMeta != null)
            return (sfMeta, false);

        sfMeta = new ScryfallCardMetadata
        {
            Id = sfCardMeta.Id.ToString(),
            CardName = name,
            Edition = edition,
            CardType = sfCardMeta.TypeLine,
            Rarity = sfCardMeta.Rarity,
            Type = ParseType(sfCardMeta.TypeLine),
            ManaValue = (int)sfCardMeta.Cmc
        };
        await _db.Set<ScryfallCardMetadata>().AddAsync(sfMeta, cancel);
        return (sfMeta, true);
    }

    public async ValueTask<ScryfallCardMetadata?> TryResolveAsync(
        string cardName,
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
            Card? sfCardMeta = null;
            try
            {
                sfCardMeta = await FindCardAsync(key);
            }
            catch { }
            if (sfCardMeta != null)
            {
                // Now we create and add this entity if not found
                if (sfMeta == null)
                {
                    (sfMeta, _) = await TryAddMetadataAsync(key.cardName, key.edition, sfCardMeta, cancel);
                }
                
                // New or existing, update these properties if different

                // Collector #
                if (sfMeta.CollectorNumber != sfCardMeta.CollectorNumber)
                    sfMeta.CollectorNumber = sfCardMeta.CollectorNumber;

                // Type
                if (sfMeta.Type == null)
                    sfMeta.Type = ParseType(sfCardMeta.TypeLine);

                // Mana Value
                if (sfMeta.ManaValue == null)
                    sfMeta.ManaValue = (int)sfCardMeta.Cmc;

                // Small card image (front face)
                if (sfMeta.ImageSmall == null)
                {
                    byte[]? imageSmall = null;
                    var smallUri = sfCardMeta.GetFrontFaceImageUri(IMG_SIZE_SMALL);
                    if (smallUri != null)
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

                // Small card image (back face)
                if (sfMeta.BackImageSmall == null)
                {
                    byte[]? imageSmall = null;
                    var smallUri = sfCardMeta.GetBackFaceImageUri(IMG_SIZE_LARGE);
                    if (smallUri != null)
                    {
                        try
                        {
                            imageSmall = await _http.GetByteArrayAsync(smallUri, cancel);
                            sfMeta.BackImageSmall = imageSmall;
                            this.ScryfallSmallImageFetches++;
                        }
                        catch { }
                    }
                }

                // Large card image (front face)
                if (sfMeta.ImageLarge == null)
                {
                    byte[]? imageLarge = null;
                    var smallUri = sfCardMeta.GetFrontFaceImageUri(IMG_SIZE_LARGE);
                    if (smallUri != null)
                    {
                        try
                        {
                            imageLarge = await _http.GetByteArrayAsync(smallUri, cancel);
                            sfMeta.ImageLarge = imageLarge;
                            this.ScryfallSmallImageFetches++;
                        }
                        catch { }
                    }
                }

                // Large card image (back face)
                if (sfMeta.BackImageLarge == null)
                {
                    byte[]? imageLarge = null;
                    var smallUri = sfCardMeta.GetBackFaceImageUri(IMG_SIZE_LARGE);
                    if (smallUri != null)
                    {
                        try
                        {
                            imageLarge = await _http.GetByteArrayAsync(smallUri, cancel);
                            sfMeta.BackImageLarge = imageLarge;
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

    const string IMG_SIZE_SMALL = "small";
    const string IMG_SIZE_LARGE = "large";

    static string ParseType(string typeLine)
    {
        // No, you're not seeing things. These are 2 distinct hyphens thanks to the power of unicode!
        // And scryfall insists on putting the unicode hyphen in its typeline!
        // But just in case, we'll split on both unicode and ascii variants.
        var tokens = typeLine.Split('—', '-');
        return tokens[0].Trim();
    }
}
