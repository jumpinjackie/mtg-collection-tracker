using Microsoft.EntityFrameworkCore;
using MtgCollectionTracker.Data;
using ScryfallApi.Client;
using ScryfallApi.Client.Models;

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

    public int ScryfallSmallImageFetches { get; private set; } = 0;

    static string? NullIf(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;

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
                var sfCards = await _scryfallApiClient.Cards.Search(key.cardName, pageNo, new ScryfallApi.Client.Models.SearchOptions()
                {
                    IncludeMultilingual = true,
                    Mode = ScryfallApi.Client.Models.SearchOptions.RollupMode.Prints
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
            ScryfallApi.Client.Models.Card? sfCardMeta = null;
            try
            {
                sfCardMeta = await FindCardAsync(key);
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
                        Rarity = sfCardMeta.Rarity,
                        Type = ParseType(sfCardMeta.TypeLine),
                        ManaValue = (int)sfCardMeta.Cmc
                    };
                    await _db.Set<ScryfallCardMetadata>().AddAsync(sfMeta, cancel);
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
                    var smallUri = sfCardMeta.GetFrontFaceImageUri("small");
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
                if (sfMeta.BackImageSmall == null )
                {
                    byte[]? imageSmall = null;
                    var smallUri = sfCardMeta.GetBackFaceImageUri("small");
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

                _dict.Add(key, sfMeta);
            }
        }

        return sfMeta;
    }

    static string ParseType(string typeLine)
    {
        // No, you're not seeing things. These are 2 distinct hyphens thanks to the power of unicode!
        // And scryfall insists on putting the unicode hyphen in its typeline!
        // But just in case, we'll split on both unicode and ascii variants.
        var tokens = typeLine.Split('—', '-');
        return tokens[0].Trim();
    }
}
