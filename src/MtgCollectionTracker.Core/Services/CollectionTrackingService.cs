using Microsoft.EntityFrameworkCore;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Data;
using ScryfallApi.Client;
using StrongInject;
using System.Data;
using System.Linq.Expressions;
using System.Text;

namespace MtgCollectionTracker.Core.Services;

public class CollectionTrackingService : ICollectionTrackingService
{
    // Make no assumptions about lifecycles of service consumers. Every dbcontext access
    // *must* be a transient unit of work, and the only way to enforce that is to make this
    // dbcontext a Func<Owned<T>> and to leverage C# using blocks to ensure dbcontext disposal
    // when done
    readonly Func<Owned<CardsDbContext>> _db;

    public CollectionTrackingService(Func<Owned<CardsDbContext>> db)
    {
        _db = db;
    }

    static string FixCardName(string name)
    {
        // This card doesn't exist on paper. Use the paper name
        if (string.Equals(name, "\"Name Sticker\" Goblin", StringComparison.OrdinalIgnoreCase))
            return "_____ Goblin";

        // Split-cards. / should hopefully be a reserved (no pun intended) character
        if (name.IndexOf('/') >= 0 && name.IndexOf("//") < 0)
            name = string.Join(" // ", name.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()));

        return name.Trim();
    }

    public IEnumerable<CardLanguageModel> GetLanguages()
    {
        using var db = _db.Invoke();
        return db.Value.Set<CardLanguage>().Select(lang => new CardLanguageModel(lang.Code, lang.PrintedCode, lang.Name)).ToList();
    }

    /// <summary>
    /// Checks the shortfall for the given card name.
    /// </summary>
    /// <param name="cardName">The name of the card</param>
    /// <param name="wantQty">The quantity you are after</param>
    /// <param name="noProxies">If true, excludes proxies and non-legal sets from the card search</param>
    /// <param name="sparesOnly">If true, excludes from the card search skus that are part of an existing deck</param>
    /// <returns>If positive, there is a shortfall in your collection. If 0 or negative, the requested <paramref name="wantQty"/> can be met by your collection</returns>
    public async ValueTask<CheckQuantityResult> CheckQuantityShortfallAsync(string cardName, int wantQty, bool noProxies, bool sparesOnly)
    {
        using var db = _db.Invoke();

        string? suggestedName = null;
        var searchName = FixCardName(cardName);
        var skus = db.Value.Cards
            .Include(c => c.Deck)
            .Include(c => c.Container)
            // TODO: Need a dedicated normalized and ascii-folded column to search against
            .Where(s => s.CardName.ToLower() == searchName.ToLower());
        if (noProxies)
            skus = skus.Where(s => !DeckPrinter.IsProxyEdition(s.Edition));
        if (sparesOnly) // A spare is any sku not belonging to an existing deck
            skus = skus.Where(s => s.DeckId == null);

        HashSet<string> fromDeckNames = new();
        HashSet<string> fromContainerNames = new();
        var matchingSkus = new List<CardSku>();
        matchingSkus.AddRange(skus);
        // No match, card name may need fixing up
        if (matchingSkus.Count == 0)
        {
            // See if this is the front-side of a mdfc/adventure card
            var searchName2 = searchName + " //";
            skus = db.Value.Cards
                .Include(c => c.Deck)
                .Include(c => c.Container)
                // TODO: Need a dedicated normalized and ascii-folded column to search against
                .Where(s => s.CardName.ToLower().StartsWith(searchName2.ToLower()));

            matchingSkus.AddRange(skus);

            // If this produced results, accept the replacement suggested name if the matches yield only
            // one single distinct card name
            if (matchingSkus.Count > 0)
            {
                var names = new HashSet<string>(matchingSkus.Select(s => s.CardName));
                if (names.Count == 1)
                    suggestedName = names.First();
            }
        }
        else
        {
            // If the original input card name was fixed up, return the fixed name as suggested
            if (searchName != cardName)
                suggestedName = searchName;
        }

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
        return new (wantQty - availableTotal, fromDeckNames, fromContainerNames, suggestedName);
    }

    public IEnumerable<ContainerSummaryModel> GetContainers()
    {
        using var db = _db.Invoke();
        return db.Value
            .Containers
            .OrderBy(c => c.Name)
            .Select(c => new ContainerSummaryModel
            {
                Id = c.Id,
                Name = c.Name,
                Total = c.Cards.Sum(c => c.Quantity)
            })
            .ToList();
    }

    public IEnumerable<DeckSummaryModel> GetDecks(DeckFilterModel? filter)
    {
        Expression<Func<Deck, bool>> predicate = (filter == null || !filter.Formats.Any())
            ? d => true
            : d => filter.Formats.Contains(d.Format);
        Expression<Func<Deck, bool>> predicate2 = (filter == null || !(filter.Ids?.Any() == true))
            ? d => true
            : d => filter.Ids.Contains(d.Id);

        using var db = _db.Invoke();
        return db.Value
            .Decks
            .Include(d => d.Container)
            .Where(predicate)
            .Where(predicate2)
            .OrderBy(d => d.Name)
            .Select(d => new DeckSummaryModel
            {
                Id = d.Id,
                Name = d.Name,
                ContainerName = d.Container!.Name,
                Format = d.Format,
                MaindeckTotal = d.Cards.Where(c => !c.IsSideboard).Sum(c => c.Quantity),
                SideboardTotal = d.Cards.Where(c => c.IsSideboard).Sum(c => c.Quantity)
            })
            .ToList();
    }

    public async ValueTask<CardSkuModel> GetCardSkuByIdAsync(int id, CancellationToken cancel)
    {
        using var db = _db.Invoke();
        var sku = await db.Value
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
        using var db = _db.Invoke();
        IQueryable<CardSku> queryable = db.Value
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

        if (query.Tags?.Any() == true)
            queryable = queryable.Where(c => c.Tags.Any(t => query.Tags.Contains(t.Name)));

        if (query.UnParented)
            queryable = queryable.Where(c => c.ContainerId == null && c.DeckId == null);

        if (query.NoProxies)
            queryable = queryable.Where(DeckPrinter.IsNotProxyEditionExpr);

        if (query.NotInDecks)
            queryable = queryable.Where(c => c.DeckId == null);
        else if (query.DeckIds?.Length > 0)
            queryable = queryable.Where(c => c.DeckId != null && query.DeckIds.Contains((int)c.DeckId));

        var bq = queryable;
        if (query.IncludeScryfallMetadata)
            bq = queryable.Include(c => c.Scryfall);

        return ToCardSkuModel(bq.OrderBy(c => c.CardName)).ToList();
    }

    private IQueryable<CardSkuModel> ToCardSkuModel(IQueryable<CardSku> skus)
    {
        return skus.Select(c => new CardSkuModel
        {
            CardName = c.CardName,
            Comments = c.Comments,
            Condition = c.Condition,
            ContainerId = c.ContainerId,
            ContainerName = c.Container != null ? c.Container.Name + " (" + c.ContainerId + ")" : null,
            DeckId = c.DeckId,
            DeckName = c.Deck != null ? c.Deck.Name + " (" + c.DeckId + ")" : null,
            Edition = c.Edition,
            Tags = c.Tags.Select(t => t.Name).ToArray(),
            Id = c.Id,
            IsFoil = c.IsFoil,
            IsLand = c.IsLand,
            IsSideboard = c.IsSideboard,
            Language = c.Language != null ? c.Language.Code : "en",
            CollectorNumber = c.CollectorNumber,
            Quantity = c.Quantity,
            ScryfallId = c.ScryfallId,
            // A double-faced card has back-face image, but if we haven't loaded SF metadata
            // for this card yet, then a DFC should have '//' in its card name
            IsDoubleFaced = c.Scryfall != null
                ? c.Scryfall.BackImageSmall != null
                : c.CardName.Contains(" // ")
        });
    }

    public PaginatedCardSkuModel GetCardsForContainer(int containerId, FetchContainerPageModel options)
    {
        using var db = _db.Invoke();
        IQueryable<CardSku> queryable = db.Value
            .Cards
            .Include(c => c.Deck)
            .Include(c => c.Container)
            .Include(c => c.Scryfall)
            .Include(c => c.Language)
            .Where(c => c.ContainerId == containerId);

        if (options.ShowOnlyMissingMetadata)
            queryable = queryable.Where(c => c.ScryfallId == null);

        var total = queryable.Count();

        // Simulate a 3x4 binder by default
        const int DEFAULT_PAGE_SIZE = 12;
        var size = (options.PageSize ?? DEFAULT_PAGE_SIZE);
        var skip = options.PageNumber * size;

        return new()
        {
            PageNumber = options.PageNumber,
            PageSize = size,
            Total = total,
            Items = ToCardSkuModel(queryable.OrderBy(c => c.CardName).Skip(skip).Take(size)).ToList()
        };
    }

    public async ValueTask<IEnumerable<WishlistItemModel>> UpdateWishlistMetadataAsync(ICollection<int> ids, IScryfallApiClient scryfallApiClient, UpdateCardMetadataProgressCallback? callback, CancellationToken cancel)
    {
        using var db = _db.Invoke();
        var cards = new List<WishlistItemModel>(ids.Count);
        var wishlist = db.Value.WishlistItems
            .Include(c => c.Scryfall)
            .Include(c => c.Language)
            .Where(c => ids.Contains(c.Id));

        var resolver = new ScryfallMetadataResolver(db.Value, scryfallApiClient);
        foreach (var w in wishlist)
        {
            await w.ApplyScryfallMetadataAsync(resolver, true, cancel);
            cards.Add(WishListItemToModel(w));

            if (callback != null)
            {
                if (cards.Count % callback.ReportFrequency == 0)
                {
                    callback.OnProgress?.Invoke(cards.Count, ids.Count);
                }
            }
        }

        await db.Value.SaveChangesAsync(cancel);

        System.Diagnostics.Debug.WriteLine($"SF stats (cache hits: {resolver.CacheHits}, api: {resolver.ScryfallApiCalls}, small img: {resolver.ScryfallSmallImageFetches})");

        return cards;
    }

    public async ValueTask<IEnumerable<CardSkuModel>> UpdateCardMetadataAsync(ICollection<int> ids, IScryfallApiClient scryfallApiClient, UpdateCardMetadataProgressCallback? callback, CancellationToken cancel)
    {
        using var db = _db.Invoke();
        var cards = new List<CardSkuModel>(ids.Count);
        var skus = db.Value.Cards
            .Include(c => c.Scryfall)
            .Include(c => c.Deck)
            .Include(c => c.Container)
            .Include(c => c.Language)
            .Where(c => ids.Contains(c.Id));

        var resolver = new ScryfallMetadataResolver(db.Value, scryfallApiClient);
        foreach (var c in skus)
        {
            await c.ApplyScryfallMetadataAsync(resolver, true, cancel);
            cards.Add(CardSkuToModel(c));

            if (callback != null)
            {
                if (cards.Count % callback.ReportFrequency == 0)
                {
                    callback.OnProgress?.Invoke(cards.Count, ids.Count);
                }
            }
        }

        await db.Value.SaveChangesAsync(cancel);

        System.Diagnostics.Debug.WriteLine($"SF stats (cache hits: {resolver.CacheHits}, api: {resolver.ScryfallApiCalls}, small img: {resolver.ScryfallSmallImageFetches})");

        return cards;
    }

    public async ValueTask<(CardSkuModel sku, bool wasMerged)> RemoveFromDeckAsync(RemoveFromDeckInputModel model)
    {
        using var db = _db.Invoke();
        bool wasMerged = false;
        Container? container = null;
        if (model.ContainerId.HasValue)
        {
            container = await db.Value.Containers.FirstOrDefaultAsync(c => c.Id == model.ContainerId.Value);
            if (container == null)
                throw new Exception("Container not found");
        }
        var sku = await db.Value.Cards.Include(c => c.Deck).FirstOrDefaultAsync(s => s.Id == model.CardSkuId);
        if (sku == null)
            throw new Exception("Card sku not found");

        if (model.Quantity > sku.Quantity)
            throw new Exception($"The specified quantiy {model.Quantity} to remove is greater than the sku quantity of {sku.Quantity}");

        Expression<Func<CardSku, bool>> skuPredicate = c => c.DeckId == null && c.ContainerId == null;
        if (container != null)
            skuPredicate = c => c.DeckId == null && c.ContainerId == container.Id;
        var mergeSku = await db.Value.Cards
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

            await db.Value.Cards.AddAsync(newSku);

            theSku = newSku;
        }
        else //Add quantity to this existing sku
        {
            mergeSku.Quantity += model.Quantity;

            var rSku = sku.RemoveQuantity(model.Quantity);
            if (sku == rSku) // We removed entire quantity, then this sku needs to be removed
            {
                db.Value.Cards.Remove(sku);
            }

            theSku = mergeSku;

            wasMerged = true;
        }

        await db.Value.SaveChangesAsync();
        var ent = db.Value.Entry(theSku);
        ent.Reference(p => p.Container).Load();
        ent.Reference(p => p.Deck).Load();
        ent.Reference(p => p.Language).Load();
        ent.Reference(p => p.Scryfall).Load();

        return (CardSkuToModel(theSku), wasMerged);
    }

    public async ValueTask<CardSkuModel> AddToDeckAsync(AddToDeckInputModel model)
    {
        if (model.Quantity < 0)
            throw new ArgumentException("Quantity is less than zero");

        using var db = _db.Invoke();
        var deck = await db.Value.Decks.Include(c => c.Container).FirstOrDefaultAsync(d => d.Id == model.DeckId);
        if (deck == null)
            throw new Exception("Deck not found");
        var sku = await db.Value.Cards.Include(c => c.Deck).FirstOrDefaultAsync(s => s.Id == model.CardSkuId);
        if (sku == null)
            throw new Exception("Card sku not found");

        if (model.Quantity > sku.Quantity)
            throw new InvalidOperationException($"The specified quantiy {model.Quantity} cannot be satisfied by this sku, which has a quantity of {sku.Quantity}");

        if (sku.Deck != null)
            throw new InvalidOperationException($"The given sku already belongs to deck: {sku.Deck.Name}");

        var sbTotal = deck.Cards
            .Where(c => c.IsSideboard)
            .Sum(c => c.Quantity);

        if (model.IsSideboard && sbTotal + model.Quantity > DeckPrinter.SIDEBOARD_LIMIT)
            throw new InvalidOperationException($"This operation would go over the sideboard limit");

        var newSku = sku.RemoveQuantity(model.Quantity);
        newSku.Deck = deck;
        newSku.Container = null; // Container (if any) is inferred by the container of the deck
        newSku.IsSideboard = model.IsSideboard;

        await db.Value.SaveChangesAsync();
        var ent = db.Value.Entry(newSku);
        ent.Reference(p => p.Container).Load();
        ent.Reference(p => p.Deck).Load();

        return CardSkuToModel(newSku);
    }

    static WishlistItemModel WishListItemToModel(WishlistItem w)
    {
        return new WishlistItemModel
        {
            CardName = w.CardName,
            Condition = w.Condition,
            Edition = w.Edition,
            Tags = w.Tags.Select(t => t.Name).ToArray(),
            Id = w.Id,
            IsFoil = w.IsFoil,
            IsLand = w.IsLand,
            Language = w.Language?.Code ?? "en",
            CollectorNumber = w.CollectorNumber,
            Quantity = w.Quantity,
            ScryfallId = w.ScryfallId,
            // A double-faced card has back-face image
            IsDoubleFaced = w.Scryfall?.BackImageSmall != null,
            Offers = w.OfferedPrices?.Select(o => new VendorOfferModel
            {
                VendorId = o.VendorId,
                VendorName = o.Vendor.Name,
                AvailableStock = o.AvailableStock,
                Price = o.Price,
                Notes = o.Notes
            })?.ToList() ?? []
        };
    }

    static CardSkuModel CardSkuToModel(CardSku c)
    {
        return new CardSkuModel
        {
            CardName = c.CardName,
            Comments = c.Comments,
            Condition = c.Condition,
            ContainerId = c.ContainerId,
            ContainerName = c.Container?.Name,
            DeckId = c.DeckId,
            DeckName = c.Deck?.Name,
            Edition = c.Edition,
            Tags = c.Tags.Select(t => t.Name).ToArray(),
            Id = c.Id,
            IsFoil = c.IsFoil,
            IsLand = c.IsLand,
            IsSideboard = c.IsSideboard,
            Language = c.Language?.Code ?? "en",
            CollectorNumber = c.CollectorNumber,
            Quantity = c.Quantity,
            ScryfallId = c.ScryfallId,
            // A double-faced card has back-face image
            IsDoubleFaced = c.Scryfall?.BackImageSmall != null
        };
    }

    public IEnumerable<WishlistItemModel> GetWishlistItems(WishlistItemFilter filter)
    {
        using var db = _db.Invoke();

        Expression<Func<WishlistItem, bool>> predicate = w => true;
        if (filter?.Tags != null)
            predicate = w => w.Tags.Any(t => filter.Tags.Contains(t.Name));

        return db.Value.WishlistItems
            .Include(w => w.Scryfall)
            .Where(predicate)
            .Select(w => new WishlistItemModel
            {
                CardName = w.CardName,
                Condition = w.Condition,
                Edition = w.Edition,
                Tags = w.Tags.Select(t => t.Name).ToArray(),
                Id = w.Id,
                IsFoil = w.IsFoil,
                IsLand = w.IsLand,
                Language = w.Language!.Code ?? "en",
                CollectorNumber = w.CollectorNumber,
                Quantity = w.Quantity,
                ScryfallId = w.ScryfallId,
                // A double-faced card has back-face image
                IsDoubleFaced = w.Scryfall!.BackImageSmall != null,
                Offers = w.OfferedPrices.Select(o => new VendorOfferModel
                {
                    VendorId = o.VendorId,
                    VendorName = o.Vendor.Name,
                    AvailableStock = o.AvailableStock,
                    Price = o.Price,
                    Notes = o.Notes
                }).ToList()
            }).ToList();
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

        using var db = _db.Invoke();
        var resolver = new ScryfallMetadataResolver(db.Value, scryfallClient);

        var witems = new List<WishlistItem>();
        var cancel = CancellationToken.None;
        foreach (var sku in cards)
        {
            await sku.ApplyScryfallMetadataAsync(resolver, false, cancel);
            await db.Value.WishlistItems.AddAsync(sku);
            witems.Add(sku);
        }

        var res = await db.Value.SaveChangesAsync();

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
        using var db = _db.Invoke();
        if (containerId.HasValue)
            cnt = await db.Value.Containers.FindAsync(containerId.Value);
        if (deckId.HasValue)
            dck = await db.Value.Decks.Include(d => d.Container).FirstOrDefaultAsync(d => d.Id == deckId.Value);

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

        var resolver = new ScryfallMetadataResolver(db.Value, scryfallClient);

        var skus = new List<CardSku>();
        var cancel = CancellationToken.None;
        foreach (var sku in cards)
        {
            await sku.ApplyScryfallMetadataAsync(resolver, false, cancel);
            await db.Value.Cards.AddAsync(sku);
            skus.Add(sku);
        }

        var res = await db.Value.SaveChangesAsync();

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
        using var db = _db.Invoke();
        await db.Value.Cards.AddAsync(c);
        await db.Value.SaveChangesAsync();

        var ent = db.Value.Entry(c);
        ent.Reference(p => p.Container).Load();
        ent.Reference(p => p.Deck).Load();

        return CardSkuToModel(c);
    }

    public async ValueTask<ContainerSummaryModel> CreateContainerAsync(string name, string? description)
    {
        using var db = _db.Invoke();
        if (await db.Value.Containers.AnyAsync(c => c.Name == name))
        {
            throw new Exception($"A container with the name ({name}) already exists");
        }

        var c = new Container { Name = name, Description = description };
        await db.Value.Containers.AddAsync(c);
        await db.Value.SaveChangesAsync();

        await db.Value.Entry(c).Collection(nameof(c.Cards)).LoadAsync();

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
        using var db = _db.Invoke();
        if (containerId.HasValue)
        {
            cnt = await db.Value.Containers.FindAsync(containerId);
            if (cnt == null)
                throw new Exception("No such container");
        }
        if (await db.Value.Decks.AnyAsync(d => d.Name == name))
        {
            throw new Exception($"A deck with the name ({name}) already exists");
        }

        var d = new Deck { Name = name, Format = format, Container = cnt };
        await db.Value.Decks.AddAsync(d);
        await db.Value.SaveChangesAsync();

        await db.Value.Entry(d).Collection(nameof(d.Cards)).LoadAsync();

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

    public string PrintDeck(int deckId, bool reportProxyUsage)
    {
        using var db = _db.Invoke();
        var deck = db.Value.Decks.Include(d => d.Cards).FirstOrDefault(d => d.Id == deckId);
        if (deck == null)
            throw new Exception("Deck not found");

        var cards = deck.Cards.ToList();
        var text = new StringBuilder();

        DeckPrinter.Print(deck.Name, deck.Format, deck.Cards, s => text.AppendLine(s), reportProxyUsage);

        return text.ToString();
    }

    public async ValueTask<CardSkuModel> DeleteCardSkuAsync(int skuId)
    {
        using var db = _db.Invoke();
        var sku = await db.Value.Cards.FindAsync(skuId);
        if (sku == null)
            throw new Exception("Sku not found");

        db.Value.Cards.Remove(sku);
        await db.Value.SaveChangesAsync();

        return CardSkuToModel(sku);
    }

    public async ValueTask<DismantleDeckResult> DismantleDeckAsync(DismantleDeckInputModel model)
    {
        Deck? deck = null;
        Container? container = null;

        using var db = _db.Invoke();
        deck = await db.Value
            .Decks
            .Include(d => d.Cards)
            .FirstOrDefaultAsync(d => d.Id == model.DeckId);

        if (deck == null)
            throw new Exception("No such deck with given id");

        if (model.ContainerId.HasValue)
        {
            container = await db.Value
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

        db.Value.Decks.Remove(deck);
        await db.Value.SaveChangesAsync();

        return new DismantleDeckResult
        {
            Removed = removed,
            ContainerName = container?.Name
        };
    }

    public async ValueTask<DeleteContainerResult> DeleteContainerAsync(DeleteContainerInputModel model)
    {
        using var db = _db.Invoke();
        var container = await db.Value.Containers
            .Include(c => c.Cards)
            .FirstOrDefaultAsync(c => c.Id == model.ContainerId);

        if (container == null)
            throw new Exception("No such container with given id");

        int unassigned = 0;
        foreach (var sku in container.Cards)
        {
            sku.Container = null;
            unassigned++;
        }
        db.Value.Containers.Remove(container);
        await db.Value.SaveChangesAsync();

        return new DeleteContainerResult
        {
            UnassignedSkuTotal = unassigned
        };
    }

    public async Task<CardSkuModel> SplitCardSkuAsync(SplitCardSkuInputModel model)
    {
        using var db = _db.Invoke();
        var sku = await db.Value
            .Cards
            .Include(c => c.Container)
            .Include(c => c.Deck)
            .FirstOrDefaultAsync(c => c.Id == model.CardSkuId);
        if (sku == null)
            throw new Exception("Card sku not found");

        if (model.Quantity <= 0)
            throw new Exception("Quantity must be greater than 0");

        if (model.Quantity >= sku.Quantity)
            throw new Exception($"Quantity must be less than {sku.Quantity}");

        var newSku = sku.RemoveQuantity(model.Quantity);
        newSku.Container = sku.Container;

        await db.Value.Cards.AddAsync(newSku);
        await db.Value.SaveChangesAsync();

        var ent = db.Value.Entry(newSku);
        ent.Reference(p => p.Container).Load();
        ent.Reference(p => p.Deck).Load();

        return CardSkuToModel(newSku);
    }

    public async ValueTask<int> UpdateCardSkuAsync(UpdateCardSkuInputModel model, IScryfallApiClient? scryfallApiClient, CancellationToken cancel)
    {
        if (model.Quantity.HasValue && model.Quantity <= 0)
            throw new Exception("Quantity cannot be 0");

        using var db = _db.Invoke();
        var skus = db.Value.Cards.Where(c => model.Ids.Contains(c.Id));
        ScryfallMetadataResolver? resolver = null;
        if (scryfallApiClient != null)
            resolver = new ScryfallMetadataResolver(db.Value, scryfallApiClient);

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
            if (model.IsFoil.HasValue)
                sku.IsFoil = model.IsFoil.Value;
            if (model.IsSideboard.HasValue)
                sku.IsSideboard = model.IsSideboard.Value;

            if (model.ApplyTags)
                sku.SyncTags(model.Tags ?? []);

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

        var res = await db.Value.SaveChangesAsync();

        return res;
    }

    record struct CardIdentityKey(string name, string edition, string? language, CardCondition? condition, string? comments);
    record struct ContainerIdentityKey(string containerId, string deckId);

    public async ValueTask<(int skusUpdated, int skusRemoved)> ConsolidateCardSkusAsync(ConsolidateCardSkusInputModel model)
    {
        using var db = _db.Invoke();
        IQueryable<CardSku> cards = db.Value.Cards;
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
                db.Value.Cards.Remove(sku);
                skusRemoved++;
            }
            skusUpdated++;
        }

        await db.Value.SaveChangesAsync();

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
        using var db = _db.Invoke();
        return new CollectionSummaryModel
        {
            SkuTotal = db.Value.Cards.Count(),
            ProxyTotal = db.Value.Cards.Where(sku => sku.Edition == "PROXY").Sum(sku => sku.Quantity),
            CardTotal = db.Value.Cards.Sum(sku => sku.Quantity),
            DeckTotal = db.Value.Decks.Count(),
            ContainerTotal = db.Value.Containers.Count()
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
        using var db = _db.Invoke();
        db.Value.Vendors.RemoveRange(db.Value.Vendors.Where(v => toRemove.Contains(v.Id)));
        db.Value.Vendors.AddRange(toAdd);
        await db.Value.SaveChangesAsync();
        return (toAdd.Count, toRemove.Count);
    }

    public IEnumerable<VendorModel> GetVendors()
    {
        using var db = _db.Invoke();
        return db.Value.Vendors.Select(v => new VendorModel { Id = v.Id, Name = v.Name }).ToList();
    }

    public async ValueTask<WishlistItemModel> DeleteWishlistItemAsync(int id)
    {
        using var db = _db.Invoke();
        var item = await db.Value.WishlistItems.FindAsync(id);
        if (item == null)
            throw new Exception("Wishlist item not found");

        db.Value.WishlistItems.Remove(item);
        await db.Value.SaveChangesAsync();

        return WishListItemToModel(item);
    }

    public async ValueTask<WishlistItemModel> UpdateWishlistItemAsync(UpdateWishlistItemInputModel model, IScryfallApiClient? scryfallApiClient, CancellationToken cancel)
    {
        if (model.Quantity.HasValue && model.Quantity <= 0)
            throw new Exception("Quantity cannot be 0");

        using var db = _db.Invoke();
        var wi = await db.Value.WishlistItems
            .Include(w => w.Scryfall)
            .Include(w => w.OfferedPrices)
                .ThenInclude(o => o.Vendor)
            .FirstOrDefaultAsync(w => w.Id == model.Id, cancel);
        if (wi == null)
            throw new Exception("Item not found");

        ScryfallMetadataResolver? resolver = null;
        if (scryfallApiClient != null)
            resolver = new ScryfallMetadataResolver(db.Value, scryfallApiClient);

        if (model.CardName != null)
            wi.CardName = model.CardName;
        if (model.Condition != null)
            wi.Condition = model.Condition;
        if (model.Edition != null)
            wi.Edition = model.Edition;
        if (model.Language != null)
            wi.LanguageId = model.Language;
        if (model.Quantity != null)
            wi.Quantity = model.Quantity.Value;
        if (model.CollectorNumber != null)
            wi.CollectorNumber = model.CollectorNumber;
        if (model.IsFoil.HasValue)
            wi.IsFoil = model.IsFoil.Value;

        if (model.ApplyTags)
            wi.SyncTags(model.Tags ?? []);

        if (model.VendorOffers != null)
        {
            foreach (var off in model.VendorOffers)
            {
                var currentOffer = wi.OfferedPrices.FirstOrDefault(o => o.VendorId == off.VendorId);
                if (currentOffer == null)
                {
                    wi.OfferedPrices.Add(new VendorPrice
                    {
                        VendorId = off.VendorId,
                        AvailableStock = off.Available,
                        Price = off.Price,
                        Notes = off.Notes
                    });
                }
                else
                {
                    currentOffer.VendorId = off.VendorId;
                    currentOffer.AvailableStock = off.Available;
                    currentOffer.Price = off.Price;
                    currentOffer.Notes = off.Notes;
                }
            }
        }

        if (resolver != null)
        {
            await wi.ApplyScryfallMetadataAsync(resolver, true, cancel);
        }

        await db.Value.SaveChangesAsync();

        foreach (var offer in wi.OfferedPrices)
        {
            await db.Value.Entry(offer)
                .Reference(nameof(VendorPrice.Vendor))
                .LoadAsync();
        }

        return WishListItemToModel(wi);
    }

    public WishlistSpendSummaryModel GetWishlistSpend()
    {
        using var db = _db.Invoke();
        var items = db.Value.Set<WishlistItem>()
            .Include(w => w.OfferedPrices)
                .ThenInclude(p => p.Vendor)
            .Where(w => w.OfferedPrices.Count > 0);

        var vendors = new HashSet<string>();
        var isComplete = true;
        decimal total = 0;

        foreach (var item in items)
        {
            var (subTotal, v, c) = item.OfferedPrices.ComputeBestPrice(item.Quantity);
            vendors.UnionWith(v.Select(vndr => vndr.Name));
            total += subTotal;
            if (!c)
            {
                isComplete = false;
            }
        }

        return new() { IsComplete = isComplete, Total = new(total), Vendors = vendors.ToArray() };
    }

    public async ValueTask<DeckModel> GetDeckAsync(int deckId, IScryfallApiClient? scryfallApiClient, CancellationToken cancel)
    {
        using var db = _db.Invoke();
        var deck = await db.Value.Set<Deck>()
            .Include(d => d.Cards)
                .ThenInclude(c => c.Scryfall)
            .FirstOrDefaultAsync(d => d.Id == deckId);

        if (deck == null)
            throw new Exception("Deck not found");

        var mainDeck = new List<DeckCardModel>();
        var sideboard = new List<DeckCardModel>();

        var resolver = new ScryfallMetadataResolver(db.Value, scryfallApiClient);
        bool bSaveChanges = false;

        foreach (var sku in deck.Cards)
        {
            if (IsIncompleteForDeckDisplay(sku))
            {
                await sku.ApplyScryfallMetadataAsync(resolver, true, cancel);
                bSaveChanges = true;
            }

            for (int i = 0; i < sku.Quantity; i++)
            {
                var card = new DeckCardModel
                {
                    SkuId = sku.Id,
                    ScryfallId = sku.ScryfallId,
                    CardName = sku.CardName,
                    Type = sku.Scryfall?.Type,
                    ManaValue = sku.Scryfall?.ManaValue ?? -1,
                    // A double-faced card has back-face image
                    IsDoubleFaced = sku.Scryfall?.BackImageSmall != null,
                    IsLand = sku.IsLand,
                    Edition = sku.Edition
                };
                if (sku.IsSideboard)
                    sideboard.Add(card);
                else
                    mainDeck.Add(card);
            }
        }

        if (bSaveChanges)
        {
            await db.Value.SaveChangesAsync(cancel);
        }

        return new DeckModel { Name = deck.Name, Id = deck.Id, MainDeck = mainDeck.ToArray(), Sideboard = sideboard.ToArray() };
    }

    static bool IsIncompleteForDeckDisplay(CardSku sku)
    {
        return sku.Scryfall == null
            || sku.Scryfall.Type == null
            || sku.Scryfall.ImageSmall == null
            || sku.Scryfall.ManaValue == null;
    }

    public async ValueTask<MoveWishlistItemsToCollectionResult> MoveWishlistItemsToCollectionAsync(MoveWishlistItemsToCollectionInputModel model)
    {
        using var db = _db.Invoke();
        var items = db.Value.Set<WishlistItem>()
            .Include(wi => wi.Scryfall)
            .Where(wi => model.WishlistItemIds.Contains(wi.Id))
            .ToList();

        var converted = new List<(int id, CardSku sku)>();
        foreach (var item in items)
        {
            converted.Add(new(item.Id, item.CreateSku(model.ContainerId)));
        }

        db.Value.Set<WishlistItem>().RemoveRange(items);
        await db.Value.Set<CardSku>().AddRangeAsync(converted.Select(c => c.sku));
        await db.Value.SaveChangesAsync();

        foreach (var added in converted)
        {
            await db.Value.Entry(added.sku).Reference(nameof(CardSku.Scryfall)).LoadAsync();
        }

        return new MoveWishlistItemsToCollectionResult { CreatedSkus = converted.Select(c => new WishlistItemMoveResult(c.id, CardSkuToModel(c.sku))).ToArray() };
    }

    public IEnumerable<NotesModel> GetNotes()
    {
        using var db = _db.Invoke();
        return db.Value.Notes.Select(n => new NotesModel { Id = n.Id, Notes = n.Text, Title = n.Title }).ToList();
    }

    public async ValueTask<NotesModel> UpdateNotesAsync(int? id, string? title, string notes)
    {
        Notes? n = null;
        using var db = _db.Invoke();
        if (id.HasValue)
        {
            n = await db.Value.Notes.FindAsync(id.Value);
            if (n == null)
                throw new Exception("Notes not found");
            n.Title = title;
            n.Text = notes;
        }
        else 
        {
            n = new Notes() { Title = title, Text = notes };
            await db.Value.Notes.AddAsync(n);
        }
        await db.Value.SaveChangesAsync();
        return new NotesModel
        {
            Id = n.Id,
            Title = n.Title,
            Notes = n.Text
        };
    }

    public async ValueTask<bool> DeleteNotesAsync(int id)
    {
        using var db = _db.Invoke();
        var n = await db.Value.Notes.FindAsync(id);
        if (n == null)
            return false;
        db.Value.Notes.Remove(n);
        await db.Value.SaveChangesAsync();
        return true;
    }

    public async ValueTask<Dictionary<string, ScryfallResolvedCard>> ResolveEditionsForCardsAsync(IEnumerable<string> cardNames, IScryfallApiClient client)
    {
        using var db = _db.Invoke();
        var resolved = new Dictionary<string, ScryfallResolvedCard>();
        var sfMetaResolver = new ScryfallMetadataResolver(db.Value, client);
        var cancel = CancellationToken.None;
        bool bSave = false;
        foreach (var cardName in cardNames.Distinct())
        {
            var (sfc, added) = await sfMetaResolver.ResolveEditionAsync(cardName, cancel);
            if (sfc != null)
                resolved[cardName] = sfc;

            if (added)
                bSave = true;
        }
        if (bSave)
        {
            await db.Value.SaveChangesAsync(cancel);
        }
        return resolved;
    }

    public WishlistBuyingListModel GenerateBuyingList()
    {
        using var db = _db.Invoke();
        var ret = new WishlistBuyingListModel();
        var wishlist = db.Value.Set<WishlistItem>()
            .Include(w => w.OfferedPrices)
                .ThenInclude(o => o.Vendor);
        foreach (var item in wishlist)
        {
            var (subTotal, vendors, isComplete) = item.OfferedPrices.ComputeBestPrice(item.Quantity);
            foreach (var vendor in vendors)
            {
                ret.Add(vendor.Name, new BuyingListItem(vendor.Qty, item.CardName, vendor.Price, vendor.Notes));
            }
        }
        return ret;
    }

    public IEnumerable<string> GetDeckFormats()
    {
        using var db = _db.Invoke();
        return db.Value.Decks.Where(d => d.Format != null).Select(d => d.Format).Distinct().ToList();
    }

    public bool HasOtherDecksInFormat(string format)
    {
        using var db = _db.Invoke();
        return db.Value.Decks.Any(d => d.Format == format);
    }

    private async ValueTask<Stream?> GetCardFaceImageAsync(string scryfallId, string tag, Expression<Func<ScryfallCardMetadata, byte[]?>> selector)
    {
        using var db = _db.Invoke();
        var imgBytes = await db.Value.Set<ScryfallCardMetadata>()
            .Where(m => m.Id == scryfallId)
            .Select(selector)
            .FirstOrDefaultAsync();
        if (imgBytes != null)
        {
            var stream = MemoryStreamPool.GetStream(tag, imgBytes);
            return stream;
        }
        return null;
    }

    public async ValueTask<Stream?> GetLargeFrontFaceImageAsync(string scryfallId)
    {
        return await GetCardFaceImageAsync(scryfallId, "img_front_face_large", m => m.ImageLarge);
    }

    public async ValueTask<Stream?> GetLargeBackFaceImageAsync(string scryfallId)
    {
        return await GetCardFaceImageAsync(scryfallId, "img_back_face_large", m => m.BackImageLarge);
    }

    public async ValueTask<Stream?> GetSmallFrontFaceImageAsync(string scryfallId)
    {
        return await GetCardFaceImageAsync(scryfallId, "img_front_face_small", m => m.ImageSmall);
    }

    public async ValueTask<Stream?> GetSmallBackFaceImageAsync(string scryfallId)
    {
        return await GetCardFaceImageAsync(scryfallId, "img_back_face_small", m => m.BackImageSmall);
    }

    public IEnumerable<string> GetTags()
    {
        using var db = _db.Invoke();
        return db.Value.Set<Tag>().Select(t => t.Name).ToList();
    }

    public async ValueTask<ApplyTagsResult> ApplyTagsAsync(IEnumerable<string> tags, CancellationToken cancel)
    {
        using var db = _db.Invoke();

        var tagSet = db.Value.Set<Tag>();
        var currentTags = tagSet.ToList();
        var inTags = tags.ToHashSet();
        var toAdd = new List<Tag>();
        var toRemove = new List<Tag>();

        foreach (var inTag in inTags)
        {
            if (!currentTags.Any(t => t.Name == inTag)) // Not present, add this
            {
                toAdd.Add(new Tag { Name = inTag });
            }
        }

        foreach (var eTag in currentTags)
        {
            if (!inTags.Contains(eTag.Name)) // Current tag not in new one, remove this
            {
                toRemove.Add(eTag);
            }
        }

        await tagSet.AddRangeAsync(toAdd, cancel);
        tagSet.RemoveRange(toRemove);
        await db.Value.SaveChangesAsync(cancel);

        int skuDetached = 0;
        int wishlistDetached = 0;
        // Now bulk delete these tags from all referencing SKUs and wishlist items
        // Has to be raw SQL as we can't do ExecuteDelete() on EF owned types, unless I'm mistaken.
        foreach (var t in toRemove)
        {
            skuDetached += await db.Value.Database.ExecuteSqlRawAsync("DELETE FROM [CardSkuTag] WHERE [Name] = {0}", t.Name);
            wishlistDetached += await db.Value.Database.ExecuteSqlRawAsync("DELETE FROM [WishlistItemTag] WHERE [Name] = {0}", t.Name);
        }

        return new ApplyTagsResult(toAdd.Count, toRemove.Count, skuDetached + wishlistDetached, tagSet.Select(t => t.Name).ToList());
    }
}
