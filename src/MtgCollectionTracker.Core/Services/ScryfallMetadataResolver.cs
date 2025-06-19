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

        Card? sfc = null;
        int pageNo = 0;
        while (true)
        {
            pageNo++;
            try
            {
                // Resolve scryfall metadata
                var sfCards = await _scryfallApiClient.Cards.Search(cardName, pageNo, new SearchOptions()
                {
                    IncludeMultilingual = true,
                    Mode = SearchOptions.RollupMode.Prints
                });
                this.ScryfallApiCalls++;
                if (sfCards.Data.Count > 0)
                {
                    // Get first matching oldest paper printing of card name
                    sfc = sfCards.Data.OrderBy(c => c.ReleasedAt).FirstOrDefault(c => c.Name.ToLower() == cardName.ToLower() && c.Games?.Contains("paper") == true);
                }

                // Break if no more results or found one
                if (!sfCards.HasMore || sfc != null)
                    break;
            }
            catch (ScryfallApiException se)
            {
                break;
            }
        }

        if (sfc != null)
        {
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
                bool hasValidEdition = !string.IsNullOrWhiteSpace(key.edition) && key.edition != "proxy";
                var query = !hasValidEdition
                    ? key.cardName
                    : $"set:{key.edition} {key.cardName}";

                var sfCards = await _scryfallApiClient.Cards.Search(query, pageNo, new SearchOptions()
                {
                    IncludeMultilingual = true,
                    Mode = SearchOptions.RollupMode.Prints
                });
                this.ScryfallApiCalls++;
                cards.AddRange(sfCards.Data.Where(c => string.Equals(c.Name, key.cardName, StringComparison.OrdinalIgnoreCase)));
                // If no edition specified, we can stop after this as in this case we just want the un-editioned
                // card to match to *any* printing of this card
                if (!sfCards.HasMore || (!hasValidEdition && cards.Count > 0))
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

                // Casting cost
                if (sfMeta.CastingCost == null)
                    sfMeta.CastingCost = sfCardMeta.ManaCost;

                // Oracle text
                if (sfMeta.OracleText != sfCardMeta.OracleText)
                    sfMeta.OracleText = sfCardMeta.OracleText;

                // P/T
                if (sfMeta.Power == null)
                    sfMeta.Power = sfCardMeta.Power;
                if (sfMeta.Toughness == null)
                    sfMeta.Toughness = sfCardMeta.Toughness;

                // Color
                if (sfMeta.Colors == null)
                    sfMeta.Colors = sfCardMeta.Colors;

                // Color Identity
                if (sfMeta.ColorIdentity == null)
                    sfMeta.ColorIdentity = sfCardMeta.ColorIdentity;

                // Small card image (front face)
                if (sfMeta.ImageSmallUrl == null)
                {
                    var imageUri = sfCardMeta.GetFrontFaceImageUri(IMG_SIZE_SMALL);
                    if (imageUri != null)
                    {
                        try
                        {
                            sfMeta.ImageSmallUrl = imageUri.ToString();
                            this.ScryfallSmallImageFetches++;
                        }
                        catch { }
                    }
                }

                // Small card image (back face)
                if (sfMeta.BackImageSmallUrl == null)
                {
                    var imageUri = sfCardMeta.GetBackFaceImageUri(IMG_SIZE_NORMAL);
                    if (imageUri != null)
                    {
                        try
                        {
                            sfMeta.BackImageSmallUrl = imageUri.ToString();
                            this.ScryfallSmallImageFetches++;
                        }
                        catch { }
                    }
                }

                // Large card image (front face)
                if (sfMeta.ImageLargeUrl == null)
                {
                    var imageUri = sfCardMeta.GetFrontFaceImageUri(IMG_SIZE_NORMAL);
                    if (imageUri != null)
                    {
                        try
                        {
                            sfMeta.ImageLargeUrl = imageUri.ToString();
                            this.ScryfallSmallImageFetches++;
                        }
                        catch { }
                    }
                }

                // Large card image (back face)
                if (sfMeta.BackImageLargeUrl == null)
                {
                    var imageUri = sfCardMeta.GetBackFaceImageUri(IMG_SIZE_NORMAL);
                    if (imageUri != null)
                    {
                        try
                        {
                            sfMeta.BackImageLargeUrl = imageUri.ToString();
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
    const string IMG_SIZE_NORMAL = "normal";

    static string ParseType(string typeLine)
    {
        // No, you're not seeing things. These are 2 distinct hyphens thanks to the power of unicode!
        // And scryfall insists on putting the unicode hyphen in its typeline!
        // But just in case, we'll split on both unicode and ascii variants.
        var tokens = typeLine.Split('—', '-');
        return tokens[0].Trim();
    }
}
