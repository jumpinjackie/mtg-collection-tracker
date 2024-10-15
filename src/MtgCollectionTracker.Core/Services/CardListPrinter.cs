using MtgCollectionTracker.Data;
using System.Linq.Expressions;

namespace MtgCollectionTracker.Core.Services;

public static class CardListPrinter
{
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

    public static Expression<Func<CardSku, bool>> IsProxyEditionExpr => c => proxySets.Contains(c.Edition);

    public static Expression<Func<CardSku, bool>> IsNotProxyEditionExpr => c => !proxySets.Contains(c.Edition);

    public static bool IsProxyEdition(string edition) => proxySets.Contains(edition);

    internal const int SIDEBOARD_LIMIT = 15;

    public static void PrintContainer<T>(string name, string? description, IEnumerable<T> cards, Action<string> writeLine, bool reportProxyUsage) where T : IDeckPrintableSlot
    {
        var containerTotal = cards.Sum(c => c.Quantity);
        var proxyTotal = cards.Where(c => IsProxyEdition(c.Edition)).Sum(c => c.Quantity);

        var slots = cards
            .GroupBy(c => new { c.CardName })
            .Select(grp => new { Name = grp.Key.CardName, Count = grp.Sum(c => c.Quantity), ProxyCount = grp.Where(c => IsProxyEdition(c.Edition)).Sum(c => c.Quantity) });

        writeLine($"Name: {name}");
        if (!string.IsNullOrEmpty(description))
            writeLine($"Description: {description}");
        writeLine($"Total: {containerTotal}");
        if (proxyTotal > 0)
            writeLine($"Proxy Total: {proxyTotal}");

        foreach (var item in slots)
        {
            if (item.ProxyCount > 0 && reportProxyUsage)
                writeLine($"{item.Count} {item.Name} [{item.ProxyCount} proxies]");
            else
                writeLine($"{item.Count} {item.Name}");
        }
    }

    public static void PrintDeck<T>(string deckName, string? deckFormat, IEnumerable<T> cards, Action<string> writeLine, bool reportProxyUsage) where T : IDeckPrintableSlot
    {
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

        writeLine($"Deck Name: {deckName}");
        if (!string.IsNullOrEmpty(deckFormat))
            writeLine($"Format: {deckFormat}");
        writeLine(string.Empty);

        writeLine($"// Main Deck ({mdNonLandTotal} / {mdNonLandTotal + mdLandTotal})");
        foreach (var item in mdNonLand)
        {
            if (item.ProxyCount > 0 && reportProxyUsage)
                writeLine($"{item.Count} {item.Name} [{item.ProxyCount} proxies]");
            else
                writeLine($"{item.Count} {item.Name}");
        }
        writeLine($"// Lands ({mdLandTotal} / {mdNonLandTotal + mdLandTotal})");
        foreach (var item in mdLand)
        {
            if (item.ProxyCount > 0 && reportProxyUsage)
                writeLine($"{item.Count} {item.Name} [{item.ProxyCount} proxies]");
            else
                writeLine($"{item.Count} {item.Name}");
        }

        if (sbTotal > 0)
        {
            if (sbTotal < SIDEBOARD_LIMIT)
                writeLine($"// Sideboard ({sbTotal}, {SIDEBOARD_LIMIT - sbTotal} card(s) short!)");
            else
                writeLine($"// Sideboard ({sbTotal})");
            foreach (var item in sb)
            {
                if (item.ProxyCount > 0 && reportProxyUsage)
                    writeLine($"{item.Count} {item.Name} [{item.ProxyCount} proxies]");
                else
                    writeLine($"{item.Count} {item.Name}");
            }
        }
        else
        {
            writeLine("// WARNING: This deck has no sideboard!");
        }

        if (reportProxyUsage)
        {
            if (proxyTotal > 0)
            {
                writeLine(string.Empty);
                writeLine("Proxy stats:");
                writeLine($"  {proxyTotal} cards [{((double)proxyTotal / (double)deckTotal):P2} of the deck] is proxies or originates from sets not legal for sanctioned tournaments");
                writeLine("This deck cannot be played in DCI/Wizards sanctioned tournaments");
            }
            else
            {
                writeLine(string.Empty);
                writeLine("This deck has no proxies");
            }
        }
    }
}
