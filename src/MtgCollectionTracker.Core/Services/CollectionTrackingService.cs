using Microsoft.EntityFrameworkCore;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Data;
using System.Linq.Expressions;
using System.Text;

namespace MtgCollectionTracker.Core.Services;

public class CollectionTrackingService
{
    readonly CardsDbContext _db;

    public CollectionTrackingService(CardsDbContext db)
    {
        _db = db;
    }

    const int SIDEBOARD_LIMIT = 15;

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

    public IEnumerable<CardSkuModel> GetCards(CardQueryModel query)
    {
        IQueryable<CardSku> queryable = _db
            .Cards
            .Include(c => c.Deck)
            .Include(c => c.Container);

        if (!string.IsNullOrEmpty(query.SearchFilter))
            queryable = queryable.Where(c => c.CardName.Contains(query.SearchFilter));
        if (query.ContainerIds?.Length > 0)
            queryable = queryable.Where(c => c.ContainerId != null && query.ContainerIds.Contains((int)c.ContainerId));
        if (query.NotInDecks)
            queryable = queryable.Where(c => c.DeckId == null);
        else if (query.DeckIds?.Length > 0)
            queryable = queryable.Where(c => c.DeckId != null && query.DeckIds.Contains((int)c.DeckId));

        return queryable
            .OrderBy(c => c.CardName)
            .Select(c => new CardSkuModel
            {
                CardName = c.CardName,
                Comments = c.Comments,
                Condition = c.Condition,
                ContainerName = c.Container!.Name,
                DeckName = c.Deck!.Name,
                Edition = c.Edition,
                Id = c.Id,
                IsFoil = c.IsFoil,
                IsLand = c.IsLand,
                IsSideboard = c.IsSideboard,
                Language = c.Language,
                Quantity = c.Quantity
            });
    }

    public async ValueTask<CardSkuModel> RemoveFromDeckAsync(RemoveFromDeckInputModel model)
    {
        var container = await _db.Containers.FirstOrDefaultAsync(c => c.Id == model.ContainerId);
        if (container == null)
            throw new Exception("Container not found");
        var sku = await _db.Cards.Include(c => c.Deck).FirstOrDefaultAsync(s => s.Id == model.CardSkuId);
        if (sku == null)
            throw new Exception("Card sku not found");

        if (model.Quantity > sku.Quantity)
            throw new Exception($"The specified quantiy {model.Quantity} to remove is greater than the sku quantity of {sku.Quantity}");

        var mergeSku = await _db.Cards
            .Where(c => c.DeckId == null && c.ContainerId == model.ContainerId)
            // Must match on [card name / edition / language / condition]
            .Where(c => c.CardName == sku.CardName && c.Edition == sku.Edition && c.Language == sku.Language && c.Condition == sku.Condition)
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

            theSku = newSku;
        }
        else //Add quantity to this existing sku
        {
            mergeSku.Quantity += model.Quantity;
            theSku = mergeSku;
        }

        await _db.SaveChangesAsync();
        _db.Entry(theSku).Reference(p => p.Container).Load();
        _db.Entry(theSku).Reference(p => p.Deck).Load();

        return CardSkuToModel(theSku);
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
            throw new InvalidOperationException($"The specified quantiy {model.Quantity} cannot be satsified by this sku, which has a quantity of {sku.Quantity}");

        if (sku.Deck != null)
            throw new InvalidOperationException($"The given sku already belongs to deck: {sku.Deck.Name}");

        var sbTotal = deck.Cards
            .Where(c => c.IsSideboard)
            .Sum(c => c.Quantity);

        if (model.IsSideboard && sbTotal + model.Quantity > SIDEBOARD_LIMIT)
            throw new InvalidOperationException($"This operation would go over the sideboard limit");

        var newSku = sku.RemoveQuantity(model.Quantity);
        newSku.Deck = deck;
        //A deck resides in a container, thus this card sku resides in the same
        //container by implication.
        //
        //NOTE: This may look like a redundant property as it could be inferred by looking at the
        //deck's container. But we need this to be able cover "spares" or cards that don't belong
        //to decks
        newSku.Container = deck.Container;
        newSku.IsSideboard = model.IsSideboard;

        await _db.SaveChangesAsync();

        _db.Entry(newSku).Reference(p => p.Container).Load();
        _db.Entry(newSku).Reference(p => p.Deck).Load();

        return CardSkuToModel(newSku);
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
            Language = c.Language,
            Quantity = c.Quantity
        };
    }

    public async ValueTask<(int total, int rows)> AddMultipleToContainerOrDeckAsync(
        int? containerId,
        int? deckId,
        IEnumerable<AddToContainerInputModel> items)
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
            IsFoil = model.IsFoil,
            IsLand = model.IsLand,
            IsSideboard = model.IsSideboard,
            Language = model.Language,
            Quantity = model.Quantity
        });

        var skus = new List<CardSku>();
        foreach (var sku in cards)
        {
            await _db.Cards.AddAsync(sku);
            skus.Add(sku); ;
        }

        var res = await _db.SaveChangesAsync();
        return (skus.Sum(s => s.Quantity), skus.Count);
    }

    public async ValueTask<CardSkuModel> AddToContainerAsync(int containerId, AddToContainerInputModel model)
    {
        var c = new CardSku
        {
            CardName = model.CardName,
            Comments = model.Comments,
            Condition = model.Condition,
            ContainerId = containerId,
            //DeckId = model.DeckId,
            Edition = model.Edition,
            IsFoil = model.IsFoil,
            IsLand = model.IsLand,
            IsSideboard = model.IsSideboard,
            Language = model.Language,
            Quantity = model.Quantity
        };

        await _db.Cards.AddAsync(c);
        await _db.SaveChangesAsync();

        _db.Entry(c).Reference(p => p.Container).Load();
        _db.Entry(c).Reference(p => p.Deck).Load();

        return CardSkuToModel(c);
    }

    public async ValueTask<ContainerInfoModel> CreateContainerAsync(string name, string? description)
    {
        if (await _db.Containers.AnyAsync(c => c.Name == name))
        {
            throw new Exception($"A container with the name ({name}) already exists");
        }

        var c = new Container { Name = name, Description = description };
        await _db.Containers.AddAsync(c);
        await _db.SaveChangesAsync();

        return new ContainerInfoModel { Name = c.Name, Description = c.Description, Id = c.Id };
    }

    public async ValueTask<DeckInfoModel> CreateDeckAsync(string name, string? format, int? containerId)
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

        return new DeckInfoModel { Cards = [], ContainerName = d.Container?.Name, Format = d.Format, Name = d.Name, Id = d.Id };
    }

    static bool IsProxyEdition(string edition)
    {
        switch (edition?.ToUpper())
        {
            case "PROXY":
            // World Championship decks
            case "PTC":
            case "WC97":
            case "WC98":
            case "WC99":
            case "WC00":
            case "WC01":
            case "WC02":
            case "WC03":
            case "WC04":
            // Collector's edition
            case "CED":
            case "CEI":
            // 30th Anniversary Edition. 15 card proxy boosters, all for the low-low price of $1000 USD!
            case "30A":
                return true;
        }
        return false;
    }

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

        if (proxyTotal > 0 && reportProxyUsage)
        {
            text.AppendLine();
            text.AppendLine("Proxy stats:");
            text.AppendLine($"  {proxyTotal} cards [{((double)proxyTotal / (double)deckTotal):P2} of the deck] is proxies or originates from sets not legal for sanctioned tournaments");
            text.AppendLine("This deck cannot be played in DCI/Wizards sanctioned tournaments");
        }
        return text.ToString();
    }
}
