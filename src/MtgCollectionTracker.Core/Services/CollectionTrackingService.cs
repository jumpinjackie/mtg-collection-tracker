using Microsoft.EntityFrameworkCore;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Data;
using ScryfallApi.Client;
using System.Linq.Expressions;
using System.Text;

namespace MtgCollectionTracker.Core.Services;

public class CollectionTrackingService : ICollectionTrackingService
{
    readonly CardsDbContext _db;

    public CollectionTrackingService(CardsDbContext db)
    {
        _db = db;
    }

    const int SIDEBOARD_LIMIT = 15;

    static string FixCardName(string name)
    {
        // This card doesn't exist on paper. Use the paper name
        if (string.Equals(name, "\"Name Sticker\" Goblin", StringComparison.OrdinalIgnoreCase))
            return "_____ Goblin";

        return name;
    }

    public IEnumerable<CardLanguageModel> GetLanguages()
    {
        return _db.Set<CardLanguage>().Select(lang => new CardLanguageModel(lang.Code, lang.PrintedCode, lang.Name));
    }

    /// <summary>
    /// Checks the shortfall for the given card name.
    /// </summary>
    /// <param name="cardName">The name of the card</param>
    /// <param name="wantQty">The quantity you are after</param>
    /// <param name="noProxies">If true, excludes proxies and non-legal sets from the card search</param>
    /// <param name="sparesOnly">If true, excludes from the card search skus that are part of an existing deck</param>
    /// <returns>If positive, there is a shortfall in your collection. If 0 or negative, the requested <paramref name="wantQty"/> can be met by your collection</returns>
    public async ValueTask<(int shortAmount, HashSet<string> fromDeckNames, HashSet<string> fromContainerNames)> CheckQuantityShortfallAsync(string cardName, int wantQty, bool noProxies, bool sparesOnly)
    {
        var cn = FixCardName(cardName);
        var skus = _db.Cards.Include(c => c.Deck).Include(c => c.Container).Where(s => s.CardName == cn);
        if (noProxies)
            skus = skus.Where(s => !proxySets.Contains(s.Edition));
        if (sparesOnly) // A spare is any sku not belonging to an existing deck
            skus = skus.Where(s => s.DeckId == null);

        HashSet<string> fromDeckNames = new();
        HashSet<string> fromContainerNames = new();
        var matchingSkus = await skus.ToListAsync();
        foreach (var sku in matchingSkus)
        {
            if (sku.Deck != null)
                fromDeckNames.Add(sku.Deck.Name);
            else if (sku.Container != null)
                fromContainerNames.Add(sku.Container.Name);
            else
                fromContainerNames.Add("<un-assigned>");
        }

        var availableTotal = matchingSkus.Sum(s => s.Quantity);
        return (wantQty - availableTotal, fromDeckNames, fromContainerNames);
    }

    public IEnumerable<ContainerSummaryModel> GetContainers()
    {
        return _db
            .Containers
            .OrderBy(c => c.Name)
            .Select(c => new ContainerSummaryModel
            {
                Id = c.Id,
                Name = c.Name,
                Total = c.Cards.Sum(c => c.Quantity)
            });
    }

    public IEnumerable<DeckSummaryModel> GetDecks(string? format)
    {
        Expression<Func<Deck, bool>> predicate = string.IsNullOrEmpty(format)
            ? d => true
            : d => d.Format == format;
        return _db
            .Decks
            .Where(predicate)
            .Include(d => d.Container)
            .OrderBy(d => d.Name)
            .Select(d => new DeckSummaryModel
            {
                Id = d.Id,
                Name = d.Name,
                ContainerName = d.Container!.Name,
                Format = d.Format,
                MaindeckTotal = d.Cards.Where(c => !c.IsSideboard).Sum(c => c.Quantity),
                SideboardTotal = d.Cards.Where(c => c.IsSideboard).Sum(c => c.Quantity)
            });
    }

    public async ValueTask<CardSkuModel> GetCardSkuByIdAsync(int id, CancellationToken cancel)
    {
        var sku = await _db
            .Cards
            .Include(c => c.Deck)
            .Include(c => c.Container)
            .FirstOrDefaultAsync(c => c.Id == id, cancel);

        if (sku == null)
            throw new Exception("Not found");

        return CardSkuToModel(sku);
    }

    public IEnumerable<CardSkuModel> GetCards(CardQueryModel query)
    {
        IQueryable<CardSku> queryable = _db
            .Cards
            .Include(c => c.Deck)
            .Include(c => c.Container);

        if (!string.IsNullOrEmpty(query.SearchFilter))
        {
            var s = FixCardName(query.SearchFilter);
            queryable = queryable.Where(c => c.CardName.ToLower().Contains(s.ToLower()));
        }
        else if (query.CardSkuIds != null)
        {
            queryable = queryable.Where(c => query.CardSkuIds.Contains(c.Id));
        }
        if (query.ContainerIds?.Length > 0)
            queryable = queryable.Where(c => c.ContainerId != null && query.ContainerIds.Contains((int)c.ContainerId));

        if (query.UnParented)
            queryable = queryable.Where(c => c.ContainerId == null && c.DeckId == null);

        if (query.NoProxies)
            queryable = queryable.Where(c => !proxySets.Contains(c.Edition));

        if (query.NotInDecks)
            queryable = queryable.Where(c => c.DeckId == null);
        else if (query.DeckIds?.Length > 0)
            queryable = queryable.Where(c => c.DeckId != null && query.DeckIds.Contains((int)c.DeckId));

        var bq = queryable;
        if (query.IncludeScryfallMetadata)
            bq = queryable.Include(c => c.Scryfall);

        return ToCardSkuModel(bq.OrderBy(c => c.CardName));
    }

    private IQueryable<CardSkuModel> ToCardSkuModel(IQueryable<CardSku> skus)
    {
        return skus.Select(c => new CardSkuModel
        {
            CardName = c.CardName,
            Comments = c.Comments,
            Condition = c.Condition,
            ContainerName = c.Container != null ? c.Container.Name + " (" + c.ContainerId + ")" : null,
            DeckName = c.Deck != null ? c.Deck.Name + " (" + c.DeckId + ")" : null,
            Edition = c.Edition,
            Id = c.Id,
            IsFoil = c.IsFoil,
            IsLand = c.IsLand,
            IsSideboard = c.IsSideboard,
            Language = c.Language != null ? c.Language.Code : "en",
            CollectorNumber = c.CollectorNumber,
            Quantity = c.Quantity,
            ImageSmall = c.Scryfall!.ImageSmall,
            BackImageSmall = c.Scryfall!.BackImageSmall,
            // A double-faced card has back-face image, but if we haven't loaded SF metadata
            // for this card yet, then a DFC should have '//' in its card name
            IsDoubleFaced = c.Scryfall != null
                ? c.Scryfall.BackImageSmall != null
                : c.CardName.Contains(" // ")
        });
    }

    public PaginatedCardSkuModel GetCardsForContainer(int containerId, FetchContainerPageModel options)
    {
        IQueryable<CardSku> queryable = _db
            .Cards
            .Include(c => c.Deck)
            .Include(c => c.Container)
            .Include(c => c.Scryfall)
            .Include(c => c.Language)
            .Where(c => c.ContainerId == containerId);

        if (options.ShowOnlyMissingMetadata)
            queryable = queryable.Where(c => c.ScryfallId == null);

        var total = queryable.Count();

        // Simulate a 3x4 binder
        const int pageSize = 12;
        var skip = options.PageNumber * pageSize;

        return new()
        {
            PageNumber = options.PageNumber,
            PageSize = pageSize,
            Total = total,
            Items = ToCardSkuModel(queryable.OrderBy(c => c.CardName).Skip(skip).Take(pageSize))
        };
    }

    public async ValueTask<IEnumerable<CardSkuModel>> UpdateCardMetadataAsync(IEnumerable<int> ids, IScryfallApiClient scryfallApiClient, CancellationToken cancel)
    {
        var cards = new List<CardSkuModel>();

        var skus = _db.Cards
            .Include(c => c.Scryfall)
            .Include(c => c.Deck)
            .Include(c => c.Container)
            .Include(c => c.Language)
            .Where(c => ids.Contains(c.Id));

        var resolver = new ScryfallMetadataResolver(_db, scryfallApiClient);
        foreach (var c in skus)
        {
            await c.ApplyScryfallMetadataAsync(resolver, true, cancel);
            cards.Add(CardSkuToModel(c));
        }

        await _db.SaveChangesAsync(cancel);

        System.Diagnostics.Debug.WriteLine($"SF stats (cache hits: {resolver.CacheHits}, api: {resolver.ScryfallApiCalls}, small img: {resolver.ScryfallSmallImageFetches})");

        return cards;
    }

    public async ValueTask<(CardSkuModel sku, bool wasMerged)> RemoveFromDeckAsync(RemoveFromDeckInputModel model)
    {
        bool wasMerged = false;
        Container? container = null;
        if (model.ContainerId.HasValue)
        {
            container = await _db.Containers.FirstOrDefaultAsync(c => c.Id == model.ContainerId.Value);
            if (container == null)
                throw new Exception("Container not found");
        }
        var sku = await _db.Cards.Include(c => c.Deck).FirstOrDefaultAsync(s => s.Id == model.CardSkuId);
        if (sku == null)
            throw new Exception("Card sku not found");

        if (model.Quantity > sku.Quantity)
            throw new Exception($"The specified quantiy {model.Quantity} to remove is greater than the sku quantity of {sku.Quantity}");

        Expression<Func<CardSku, bool>> skuPredicate = c => c.DeckId == null && c.ContainerId == null;
        if (container != null)
            skuPredicate = c => c.DeckId == null && c.ContainerId == container.Id;
        var mergeSku = await _db.Cards
            .Where(skuPredicate)
            // Must match on [card name / edition / language / condition / comments]
            .Where(c => c.CardName == sku.CardName && c.Edition == sku.Edition && c.Language == sku.Language && c.Condition == sku.Condition && c.Comments == sku.Comments)
            .FirstOrDefaultAsync();

        CardSku theSku;
        if (mergeSku == null)
        {
            var newSku = sku.RemoveQuantity(model.Quantity);
            newSku.Deck = null;
            newSku.DeckId = null;
            newSku.Container = container;

            // Un-set sideboard flag as we're removing from deck
            newSku.IsSideboard = false;

            await _db.Cards.AddAsync(newSku);

            theSku = newSku;
        }
        else //Add quantity to this existing sku
        {
            mergeSku.Quantity += model.Quantity;

            var rSku = sku.RemoveQuantity(model.Quantity);
            if (sku == rSku) // We removed entire quantity, then this sku needs to be removed
            {
                _db.Cards.Remove(sku);
            }

            theSku = mergeSku;

            wasMerged = true;
        }

        await _db.SaveChangesAsync();
        _db.Entry(theSku).Reference(p => p.Container).Load();
        _db.Entry(theSku).Reference(p => p.Deck).Load();
        _db.Entry(theSku).Reference(p => p.Language).Load();
        _db.Entry(theSku).Reference(p => p.Scryfall).Load();

        return (CardSkuToModel(theSku), wasMerged);
    }

    public async ValueTask<CardSkuModel> AddToDeckAsync(AddToDeckInputModel model)
    {
        if (model.Quantity < 0)
            throw new ArgumentException("Quantity is less than zero");

        var deck = await _db.Decks.Include(c => c.Container).FirstOrDefaultAsync(d => d.Id == model.DeckId);
        if (deck == null)
            throw new Exception("Deck not found");
        var sku = await _db.Cards.Include(c => c.Deck).FirstOrDefaultAsync(s => s.Id == model.CardSkuId);
        if (sku == null)
            throw new Exception("Card sku not found");

        if (model.Quantity > sku.Quantity)
            throw new InvalidOperationException($"The specified quantiy {model.Quantity} cannot be satisfied by this sku, which has a quantity of {sku.Quantity}");

        if (sku.Deck != null)
            throw new InvalidOperationException($"The given sku already belongs to deck: {sku.Deck.Name}");

        var sbTotal = deck.Cards
            .Where(c => c.IsSideboard)
            .Sum(c => c.Quantity);

        if (model.IsSideboard && sbTotal + model.Quantity > SIDEBOARD_LIMIT)
            throw new InvalidOperationException($"This operation would go over the sideboard limit");

        var newSku = sku.RemoveQuantity(model.Quantity);
        newSku.Deck = deck;
        newSku.Container = null; // Container (if any) is inferred by the container of the deck
        newSku.IsSideboard = model.IsSideboard;

        await _db.SaveChangesAsync();

        _db.Entry(newSku).Reference(p => p.Container).Load();
        _db.Entry(newSku).Reference(p => p.Deck).Load();

        return CardSkuToModel(newSku);
    }

    static WishlistItemModel WishListItemToModel(WishlistItem w)
    {
        return new WishlistItemModel
        {
            CardName = w.CardName,
            Condition = w.Condition,
            Edition = w.Edition,
            Id = w.Id,
            IsFoil = w.IsFoil,
            IsLand = w.IsLand,
            Language = w.Language?.Code ?? "en",
            CollectorNumber = w.CollectorNumber,
            Quantity = w.Quantity,
            ImageSmall = w.Scryfall?.ImageSmall,
            BackImageSmall = w.Scryfall?.BackImageSmall,
            // A double-faced card has back-face image, but if we haven't loaded SF metadata
            // for this card yet, then a DFC should have '//' in its card name
            IsDoubleFaced = w.Scryfall?.BackImageSmall != null || w.CardName.Contains(" // ")
        };
    }

    static CardSkuModel CardSkuToModel(CardSku c)
    {
        return new CardSkuModel
        {
            CardName = c.CardName,
            Comments = c.Comments,
            Condition = c.Condition,
            ContainerName = c.Container?.Name,
            DeckName = c.Deck?.Name,
            Edition = c.Edition,
            Id = c.Id,
            IsFoil = c.IsFoil,
            IsLand = c.IsLand,
            IsSideboard = c.IsSideboard,
            Language = c.Language?.Code ?? "en",
            CollectorNumber = c.CollectorNumber,
            Quantity = c.Quantity,
            ImageSmall = c.Scryfall?.ImageSmall,
            BackImageSmall = c.Scryfall?.BackImageSmall,
            // A double-faced card has back-face image, but if we haven't loaded SF metadata
            // for this card yet, then a DFC should have '//' in its card name
            IsDoubleFaced = c.Scryfall?.BackImageSmall != null || c.CardName.Contains(" // ")
        };
    }

    public IEnumerable<WishlistItemModel> GetWishlistItems()
    {
        return _db.WishlistItems.Include(w => w.Scryfall)
            .Select(w => new WishlistItemModel
            {
                CardName = w.CardName,
                Condition = w.Condition,
                Edition = w.Edition,
                Id = w.Id,
                IsFoil = w.IsFoil,
                IsLand = w.IsLand,
                Language = w.Language!.Code ?? "en",
                CollectorNumber = w.CollectorNumber,
                Quantity = w.Quantity,
                ImageSmall = w.Scryfall!.ImageSmall,
                BackImageSmall = w.Scryfall!.BackImageSmall,
                // A double-faced card has back-face image, but if we haven't loaded SF metadata
                // for this card yet, then a DFC should have '//' in its card name
                IsDoubleFaced = w.Scryfall!.BackImageSmall != null || w.CardName.Contains(" // ")
            });
    }

    public async ValueTask<ICollection<WishlistItemModel>> AddMultipleToWishlistAsync(IEnumerable<AddToWishlistInputModel> items, IScryfallApiClient? scryfallClient)
    {
        var cards = items.Select(model => new WishlistItem
        {
            CardName = model.CardName,
            Condition = model.Condition,
            Edition = model.Edition,
            CollectorNumber = model.CollectorNumber,
            IsFoil = model.IsFoil,
            IsLand = model.IsLand,
            LanguageId = model.Language,
            Quantity = model.Quantity
        });

        var resolver = new ScryfallMetadataResolver(_db, scryfallClient);

        var witems = new List<WishlistItem>();
        var cancel = CancellationToken.None;
        foreach (var sku in cards)
        {
            await sku.ApplyScryfallMetadataAsync(resolver, false, cancel);
            await _db.WishlistItems.AddAsync(sku);
            witems.Add(sku);
        }

        var res = await _db.SaveChangesAsync();

        System.Diagnostics.Debug.WriteLine($"SF stats (cache hits: {resolver.CacheHits}, api: {resolver.ScryfallApiCalls}, small img: {resolver.ScryfallSmallImageFetches})");

        return witems.Select(WishListItemToModel).ToList();
    }

    public async ValueTask<(int total, int proxyTotal, int rows)> AddMultipleToContainerOrDeckAsync(
        int? containerId,
        int? deckId,
        IEnumerable<AddToDeckOrContainerInputModel> items,
        IScryfallApiClient? scryfallClient)
    {
        Container? cnt = null;
        Deck? dck = null;
        if (containerId.HasValue)
            cnt = await _db.Containers.FindAsync(containerId.Value);
        if (deckId.HasValue)
            dck = await _db.Decks.Include(d => d.Container).FirstOrDefaultAsync(d => d.Id == deckId.Value);

        // Always take the deck's container
        if (dck != null)
            cnt = dck.Container;

        var cards = items.Select(model => new CardSku
        {
            CardName = model.CardName,
            Comments = model.Comments,
            Condition = model.Condition,
            ContainerId = containerId,
            Deck = dck,
            Container = cnt,
            Edition = model.Edition,
            CollectorNumber = model.CollectorNumber,
            IsFoil = model.IsFoil,
            IsLand = model.IsLand,
            IsSideboard = model.IsSideboard,
            LanguageId = model.Language,
            Quantity = model.Quantity
        });

        var resolver = new ScryfallMetadataResolver(_db, scryfallClient);

        var skus = new List<CardSku>();
        var cancel = CancellationToken.None;
        foreach (var sku in cards)
        {
            await sku.ApplyScryfallMetadataAsync(resolver, false, cancel);
            await _db.Cards.AddAsync(sku);
            skus.Add(sku);
        }

        var res = await _db.SaveChangesAsync();

        System.Diagnostics.Debug.WriteLine($"SF stats (cache hits: {resolver.CacheHits}, api: {resolver.ScryfallApiCalls}, small img: {resolver.ScryfallSmallImageFetches})");

        return (skus.Sum(s => s.Quantity), skus.Where(s => s.Edition == "PROXY").Sum(s => s.Quantity), skus.Count);
    }

    public async ValueTask<CardSkuModel> AddToDeckOrContainerAsync(int? containerId, int? deckId, AddToDeckOrContainerInputModel model)
    {
        var c = new CardSku
        {
            CardName = model.CardName,
            Comments = model.Comments,
            Condition = model.Condition,
            ContainerId = containerId,
            DeckId = deckId,
            Edition = model.Edition,
            IsFoil = model.IsFoil,
            IsLand = model.IsLand,
            IsSideboard = model.IsSideboard,
            LanguageId = model.Language,
            Quantity = model.Quantity
        };

        await _db.Cards.AddAsync(c);
        await _db.SaveChangesAsync();

        _db.Entry(c).Reference(p => p.Container).Load();
        _db.Entry(c).Reference(p => p.Deck).Load();

        return CardSkuToModel(c);
    }

    public async ValueTask<ContainerSummaryModel> CreateContainerAsync(string name, string? description)
    {
        if (await _db.Containers.AnyAsync(c => c.Name == name))
        {
            throw new Exception($"A container with the name ({name}) already exists");
        }

        var c = new Container { Name = name, Description = description };
        await _db.Containers.AddAsync(c);
        await _db.SaveChangesAsync();

        await _db.Entry(c).Collection(nameof(c.Cards)).LoadAsync();

        return new ContainerSummaryModel
        {
            Id = c.Id,
            Name = c.Name,
            Total = c.Cards.Sum(c => c.Quantity)
        };
    }

    public async ValueTask<DeckSummaryModel> CreateDeckAsync(string name, string? format, int? containerId)
    {
        Container? cnt = null;
        if (containerId.HasValue)
        {
            cnt = await _db.Containers.FindAsync(containerId);
            if (cnt == null)
                throw new Exception("No such container");
        }
        if (await _db.Decks.AnyAsync(d => d.Name == name))
        {
            throw new Exception($"A deck with the name ({name}) already exists");
        }

        var d = new Deck { Name = name, Format = format, Container = cnt };
        await _db.Decks.AddAsync(d);
        await _db.SaveChangesAsync();

        await _db.Entry(d).Collection(nameof(d.Cards)).LoadAsync();

        return new DeckSummaryModel
        {
            Id = d.Id,
            Name = d.Name,
            ContainerName = d.Container?.Name,
            Format = d.Format,
            MaindeckTotal = d.Cards.Where(c => !c.IsSideboard).Sum(c => c.Quantity),
            SideboardTotal = d.Cards.Where(c => c.IsSideboard).Sum(c => c.Quantity)
        };
    }

    static HashSet<string> proxySets = [
        "PROXY",
        // World Championship decks
        "PTC",
        "WC97",
        "WC98",
        "WC99",
        "WC00",
        "WC01",
        "WC02",
        "WC03",
        "WC04",
        // Collector's edition
        "CED",
        "CEI",
        // 30th Anniversary Edition. 15 card proxy booster packs, all for the low-low price of $1000 USD a pack!
        "30A"
    ];

    static bool IsProxyEdition(string edition) => proxySets.Contains(edition);

    public string PrintDeck(int deckId, bool reportProxyUsage)
    {
        var deck = _db.Decks.Include(d => d.Cards).FirstOrDefault(d => d.Id == deckId);
        if (deck == null)
            throw new Exception("Deck not found");

        var cards = deck.Cards.ToList();
        var deckTotal = cards.Sum(c => c.Quantity);
        var proxyTotal = cards.Where(c => IsProxyEdition(c.Edition)).Sum(c => c.Quantity);
        var mdNonLandTotal = cards.Where(c => !c.IsSideboard && !c.IsLand).Sum(c => c.Quantity);
        var mdLandTotal = cards.Where(c => !c.IsSideboard && c.IsLand).Sum(c => c.Quantity);
        var sbTotal = cards.Where(c => c.IsSideboard).Sum(c => c.Quantity);

        var mdNonLand = cards.Where(c => !c.IsSideboard && !c.IsLand)
            .GroupBy(c => new { c.CardName })
            .Select(grp => new { Name = grp.Key.CardName, Count = grp.Sum(c => c.Quantity), ProxyCount = grp.Where(c => IsProxyEdition(c.Edition)).Sum(c => c.Quantity) });

        var mdLand = cards.Where(c => !c.IsSideboard && c.IsLand)
            .GroupBy(c => new { c.CardName })
            .Select(grp => new { Name = grp.Key.CardName, Count = grp.Sum(c => c.Quantity), ProxyCount = grp.Where(c => IsProxyEdition(c.Edition)).Sum(c => c.Quantity) });

        var sb = cards.Where(c => c.IsSideboard)
            .GroupBy(c => new { c.CardName })
            .Select(grp => new { Name = grp.Key.CardName, Count = grp.Sum(c => c.Quantity), ProxyCount = grp.Where(c => IsProxyEdition(c.Edition)).Sum(c => c.Quantity) });

        var text = new StringBuilder();
        text.AppendLine($"Deck Name: {deck.Name}");
        if (!string.IsNullOrEmpty(deck.Format))
            text.AppendLine($"Format: {deck.Format}");
        text.AppendLine();

        text.AppendLine($"// Main Deck ({mdNonLandTotal} / {mdNonLandTotal + mdLandTotal})");
        foreach (var item in mdNonLand)
        {
            if (item.ProxyCount > 0 && reportProxyUsage)
                text.AppendLine($"{item.Count} {item.Name} [{item.ProxyCount} proxies]");
            else
                text.AppendLine($"{item.Count} {item.Name}");
        }
        text.AppendLine($"// Lands ({mdLandTotal} / {mdNonLandTotal + mdLandTotal})");
        foreach (var item in mdLand)
        {
            if (item.ProxyCount > 0 && reportProxyUsage)
                text.AppendLine($"{item.Count} {item.Name} [{item.ProxyCount} proxies]");
            else
                text.AppendLine($"{item.Count} {item.Name}");
        }

        if (sbTotal > 0)
        {
            if (sbTotal < SIDEBOARD_LIMIT)
                text.AppendLine($"// Sideboard ({sbTotal}, {SIDEBOARD_LIMIT - sbTotal} card(s) short!)");
            else
                text.AppendLine($"// Sideboard ({sbTotal})");
            foreach (var item in sb)
            {
                if (item.ProxyCount > 0 && reportProxyUsage)
                    text.AppendLine($"{item.Count} {item.Name} [{item.ProxyCount} proxies]");
                else
                    text.AppendLine($"{item.Count} {item.Name}");
            }
        }
        else
        {
            text.AppendLine("// WARNING: This deck has no sideboard!");
        }

        if (reportProxyUsage)
        {
            if (proxyTotal > 0)
            {
                text.AppendLine();
                text.AppendLine("Proxy stats:");
                text.AppendLine($"  {proxyTotal} cards [{((double)proxyTotal / (double)deckTotal):P2} of the deck] is proxies or originates from sets not legal for sanctioned tournaments");
                text.AppendLine("This deck cannot be played in DCI/Wizards sanctioned tournaments");
            }
            else
            {
                text.AppendLine();
                text.AppendLine("This deck has no proxies");
            }
        }
        return text.ToString();
    }

    public async ValueTask<CardSkuModel> DeleteCardSkuAsync(int skuId)
    {
        var sku = await _db.Cards.FindAsync(skuId);
        if (sku == null)
            throw new Exception("Sku not found");

        _db.Cards.Remove(sku);
        await _db.SaveChangesAsync();

        return CardSkuToModel(sku);
    }

    public async ValueTask<DismantleDeckResult> DismantleDeckAsync(DismantleDeckInputModel model)
    {
        Deck? deck = null;
        Container? container = null;

        deck = await _db
            .Decks
            .Include(d => d.Cards)
            .FirstOrDefaultAsync(d => d.Id == model.DeckId);

        if (deck == null)
            throw new Exception("No such deck with given id");

        if (model.ContainerId.HasValue)
        {
            container = await _db
                .Containers
                .Include(c => c.Cards)
                .FirstOrDefaultAsync(cnt => cnt.Id == model.ContainerId.Value);

            if (container == null)
                throw new Exception("No such container with given id");
        }

        int removed = 0;

        // Assign cards in deck to container (if specified, if null they become "unassigned")
        foreach (var cardSku in deck.Cards)
        {
            cardSku.Deck = null;
            cardSku.Container = container;
            // As this is no longer part of a deck, clear the sideboard flag
            cardSku.IsSideboard = false;

            removed += cardSku.Quantity;
        }

        _db.Decks.Remove(deck);
        await _db.SaveChangesAsync();

        return new DismantleDeckResult
        {
            Removed = removed,
            ContainerName = container?.Name
        };
    }

    public async Task<CardSkuModel> SplitCardSkuAsync(SplitCardSkuInputModel model)
    {
        var sku = await _db
            .Cards
            .Include(c => c.Container)
            .Include(c => c.Deck)
            .FirstOrDefaultAsync(c => c.Id == model.CardSkuId);
        if (sku == null)
            throw new Exception("Card sku not found");

        if (sku.DeckId != null)
            throw new Exception("Card sku belongs to an existing deck and cannot be split off");

        if (model.Quantity <= 0)
            throw new Exception("Quantity must be greater than 0");

        if (model.Quantity >= sku.Quantity)
            throw new Exception($"Quantity must be less than {sku.Quantity}");

        var newSku = sku.RemoveQuantity(model.Quantity);
        newSku.Container = sku.Container;

        await _db.Cards.AddAsync(newSku);
        await _db.SaveChangesAsync();

        _db.Entry(newSku).Reference(p => p.Container).Load();
        _db.Entry(newSku).Reference(p => p.Deck).Load();

        return CardSkuToModel(newSku);
    }

    public async ValueTask<int> UpdateCardSkuAsync(UpdateCardSkuInputModel model, IScryfallApiClient? scryfallApiClient, CancellationToken cancel)
    {
        if (model.Quantity.HasValue && model.Quantity <= 0)
            throw new Exception("Quantity cannot be 0");

        var skus = _db.Cards.Where(c => model.Ids.Contains(c.Id));
        ScryfallMetadataResolver? resolver = null;
        if (scryfallApiClient != null)
            resolver = new ScryfallMetadataResolver(_db, scryfallApiClient);

        foreach (var sku in skus)
        {
            if (model.CardName != null)
                sku.CardName = model.CardName;
            if (model.Condition != null)
                sku.Condition = model.Condition;
            if (model.Comments != null)
                sku.Comments = model.Comments;
            if (model.Edition != null)
                sku.Edition = model.Edition;
            if (model.Language != null)
                sku.LanguageId = model.Language;
            if (model.Quantity != null)
                sku.Quantity = model.Quantity.Value;
            if (model.CollectorNumber != null)
                sku.CollectorNumber = model.CollectorNumber;
            if (model.DeckId != null)
                sku.DeckId = model.DeckId;
            if (model.ContainerId != null)
                sku.ContainerId = model.ContainerId;
            if (model.IsLand.HasValue)
                sku.IsLand = model.IsLand.Value;
            if (model.IsSideboard.HasValue)
                sku.IsSideboard = model.IsSideboard.Value;

            if (model.UnsetDeck)
            {
                sku.DeckId = null;
                sku.Deck = null;
            }
            if (model.UnsetContainer)
            {
                sku.ContainerId = null;
                sku.Container = null;
            }

            if (resolver != null)
            {
                await sku.ApplyScryfallMetadataAsync(resolver, true, cancel);
            }
        }

        var res = await _db.SaveChangesAsync();

        return res;
    }

    record struct CardIdentityKey(string name, string edition, string? language, CardCondition? condition, string? comments);
    record struct ContainerIdentityKey(string containerId, string deckId);

    public async ValueTask<(int skusUpdated, int skusRemoved)> ConsolidateCardSkusAsync(ConsolidateCardSkusInputModel model)
    {
        IQueryable<CardSku> cards = _db.Cards;
        if (model.DeckId.HasValue || model.ContainerId.HasValue)
        {
            if (model.DeckId.HasValue)
            {
                cards = cards.Where(c => c.DeckId == model.DeckId);
            }
            if (model.ContainerId.HasValue)
            {
                cards = cards.Where(c => c.ContainerId == model.ContainerId);
            }
        }
        else
        {
            cards = cards.Where(c => c.DeckId == null && c.ContainerId == null);
        }

        var mergeCandidates = cards
            .GroupBy(c => new CardIdentityKey(c.CardName, c.Edition, c.LanguageId, c.Condition, c.Comments))
            .Where(grp => grp.Count() > 1)
            .Select(grp => new { Items = grp.ToList() });

        // All clear. Now we can merge
        var skusRemoved = 0;
        var skusUpdated = 0;
        foreach (var grp in mergeCandidates)
        {
            var targetSku = grp.Items[0];
            var skus = grp.Items.Skip(1);

            // Update quantity of merge target
            foreach (var sku in skus)
            {
                targetSku.Quantity += sku.Quantity;
                // Now remove the merge source
                _db.Cards.Remove(sku);
                skusRemoved++;
            }
            skusUpdated++;
        }

        await _db.SaveChangesAsync();

        return (skusUpdated, skusRemoved);
    }

    public bool IsBasicLand(string cardName)
    {
        switch (cardName.ToLower())
        {
            case "plains":
            case "island":
            case "swamp":
            case "mountain":
            case "forest":
            case "snow-covered plains":
            case "snow-covered island":
            case "snow-covered swamp":
            case "snow-covered mountain":
            case "snow-covered forest":
            case "wastes":
                return true;
        }
        return false;
    }

    public CollectionSummaryModel GetCollectionSummary()
    {
        return new CollectionSummaryModel
        {
            SkuTotal = _db.Cards.Count(),
            ProxyTotal = _db.Cards.Where(sku => sku.Edition == "PROXY").Sum(sku => sku.Quantity),
            CardTotal = _db.Cards.Sum(sku => sku.Quantity),
            DeckTotal = _db.Decks.Count(),
            ContainerTotal = _db.Containers.Count()
        };
    }

    public async ValueTask<(int created, int deleted)> ApplyVendorsAsync(ApplyVendorsInputModel model)
    {
        var toRemove = new List<int>();
        var toAdd = new List<Vendor>();
        var currentVendors = GetVendors();
        foreach (var name in model.Names)
        {
            if (!currentVendors.Any(v => v.Name == name))
                toAdd.Add(new Vendor { Name = name });
        }
        foreach (var v in currentVendors)
        {
            if (!model.Names.Any(n => v.Name == n))
            {
                toRemove.Add(v.Id);
            }
        }
        _db.Vendors.RemoveRange(_db.Vendors.Where(v => toRemove.Contains(v.Id)));
        _db.Vendors.AddRange(toAdd);
        await _db.SaveChangesAsync();
        return (toAdd.Count, toRemove.Count);
    }

    public IEnumerable<VendorModel> GetVendors()
    {
        return _db.Vendors.Select(v => new VendorModel { Id = v.Id, Name = v.Name });
    }
}
