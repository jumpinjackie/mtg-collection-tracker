using CommandLine;
using ConsoleTables;
using Microsoft.Extensions.DependencyInjection;
using MtgCollectionTracker.Core.Services;

namespace MtgCollectionTracker.Console.Commands;

[Verb("can-i-build-this-deck", HelpText = "Given a decklist in MTGO format, checks if you are able to build this deck with your current collection")]
internal class CanIBuildThisDeckCommand : CommandBase
{
    [Option("decklist-path", Required = true, HelpText = "The path to the MTGO decklist")]
    public required string DecklistPath { get; set; }

    [Option("no-proxies", Required = false, HelpText = "If true, excludes proxies in your collection when determining availability")]
    public bool NoProxies { get; set; }

    [Option("spares-only", Required = false, HelpText = "If true, only considers cards in your collection that do not currently belong to any decks")]
    public bool SparesOnly { get; set; }

    [Option("ignore-sideboard", Required = false, HelpText = "If true, only checks if the main deck can be built with your current collection")]
    public bool IgnoreSideboard { get; set; }

    [Option("full-report", Required = false, HelpText = "If true, prints out all cards in a full report")]
    public bool IsFullReport { get; set; }

    [Option("ignore-basic-lands", Required = false, HelpText = "If true, ignores basic lands when checking available quantities (ie. Assume you have near-unlimited basic lands)")]
    public bool IgnoreBasicLands { get; set; }

    protected override async ValueTask<int> ExecuteInternalAsync(IServiceProvider serviceProvider)
    {
        var service = serviceProvider.GetRequiredService<CollectionTrackingService>();
        using var sr = new StreamReader(this.DecklistPath);

        var rdr = new DecklistReader();
        Func<DecklistEntry, bool> predicate = e => true;
        if (this.IgnoreSideboard)
            predicate = e => !e.IsSideboard;
        var list = rdr.ReadDecklist(sr)
            .Where(predicate)
            .GroupBy(ent => ent.CardName)
            .Select(grp => new { CardName = grp.Key, Count = grp.Sum(c => c.Quantity), Short = 0 })
            .ToList();

        var rptItems = new List<(string CardName, int Requested, int Short, string FromDecks, string FromContainers)>();

        var shortFalls = new List<(string CardName, int Requested, int Short)>();
        foreach (var card in list)
        {
            if (this.IgnoreBasicLands && service.IsBasicLand(card.CardName))
                continue;

            Stdout($"Checking availability of: {card.CardName}");
            var (shortAmt, fromDecks, fromContainers) = await service.CheckQuantityShortfallAsync(card.CardName, card.Count, this.NoProxies, this.SparesOnly);
            if (shortAmt > 0)
                shortFalls.Add((card.CardName, card.Count, shortAmt));

            // Anything more than 2 items is impractical to display on a console-rendered table cell, so just provide
            // a count summary if that's the case
            var fromDecksStr = fromDecks.Count > 2 ? $"{fromDecks.Count} different decks" : string.Join(", ", fromDecks);
            var fromContainersStr = fromContainers.Count > 2 ? $"{fromContainers.Count} different containers" : string.Join(", ", fromContainers);
            rptItems.Add((card.CardName, card.Count, shortAmt, fromDecksStr, fromContainersStr));
        }

        if (this.IsFullReport)
        {
            var table = new ConsoleTable(
                "CardName",
                "RequestedAmt",
                "ShortAmt",
                "FromDecks",
                "FromContainers");

            foreach (var (cardName, requested, shortAmt, fromDecks, fromContainers) in rptItems)
            {
                var sShort = shortAmt > 0 ? shortAmt.ToString() : string.Empty;
                // If spares only, then we're only considering containers so decks cell is not applicable
                var sFromDecks = this.SparesOnly ? "N/A" : fromDecks;
                table.AddRow((shortAmt > 0 ? "[!] " : "") + cardName, requested, sShort, sFromDecks, fromContainers);
            }
            table.Write();

            if (shortFalls.Count == 0)
            {
                Stdout("Congratulations! Your collection has the cards to build this deck");
                if (!SparesOnly)
                {
                    Stdout("Please note. You may need to dismantle one or more of your existing decks to build this one.");
                    Stdout("If you want to check if this is possible without dismantling any existing deck, re-run this command with --spares-only specified");
                }
            }
            else
            {
                Stdout("Bad news! We were short on some cards in your collection. These cards are prefixed with [!]");
                if (NoProxies)
                {
                    Stdout("You may be able to build this deck if you allow proxies");
                }
                if (SparesOnly)
                {
                    Stdout("You may be able to build this deck if you allow for cards already used in other decks");
                }
            }
        }
        else
        {
            if (shortFalls.Count == 0)
            {
                Stdout("Congratulations! Your collection has the cards to build this deck");
                if (!SparesOnly)
                {
                    Stdout("Please note. You may need to dismantle one or more of your existing decks to build this one.");
                    Stdout("If you want to check if this is possible without dismantling any existing deck, re-run this command with --spares-only specified");
                }
            }
            else
            {
                Stdout("Bad news! We were short the following cards in your collection");

                var table = new ConsoleTable(
                    "CardName",
                    "RequestedAmt",
                    "ShortAmt");

                foreach (var (cardName, requested, shortAmt) in shortFalls)
                {
                    table.AddRow(cardName, requested, shortAmt);
                }
                table.Write();

                if (NoProxies)
                {
                    Stdout("You may be able to build this deck if you allow proxies");
                }
                if (SparesOnly)
                {
                    Stdout("You may be able to build this deck if you allow for cards already used in other decks");
                }
            }
        }

        return 0;
    }
}
