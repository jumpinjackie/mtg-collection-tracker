using CommandLine;
using ConsoleTables;
using Microsoft.Extensions.DependencyInjection;
using MtgCollectionTracker.Core.Services;
using System.Text.RegularExpressions;

namespace MtgCollectionTracker.Console.Commands;

record DecklistEntry(int Quantity, string CardName, bool IsSideboard);

[Verb("can-i-build-this-deck", HelpText = "Given a decklist in MTGO format, checks if you are able to build this deck with your current collection")]
internal class CanIBuildThisDeckCommand : CommandBase
{
    [Option("decklist-path", Required = true, HelpText = "The path to the MTGO decklist")]
    public required string DecklistPath { get; set; }

    [Option("no-proxies", Required = false, HelpText = "If true, excludes proxies in your collection when determining availability")]
    public bool NoProxies { get; set; }

    [Option("spares-only", Required = false, HelpText = "If true, only considers cards in your collection that do not currently belong to any decks")]
    public bool SparesOnly { get; set; }

    protected override async ValueTask<int> ExecuteInternalAsync(IServiceProvider serviceProvider)
    {
        var service = serviceProvider.GetRequiredService<CollectionTrackingService>();
        using var sr = new StreamReader(this.DecklistPath);

        var list = ReadDecklist(sr)
            .GroupBy(ent => ent.CardName)
            .Select(grp => new { CardName = grp.Key, Count = grp.Sum(c => c.Quantity), Short = 0 })
            .ToList();

        var shortFalls = new List<(string CardName, int Requested, int Short)>();
        foreach (var card in list)
        {
            Stdout($"Checking availability of: {card.CardName}");
            var shortAmt = await service.CheckQuantityShortfallAsync(card.CardName, card.Count, this.NoProxies, this.SparesOnly);
            if (shortAmt > 0)
                shortFalls.Add((card.CardName, card.Count, shortAmt));
        }

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

        return 0;
    }

    static Regex smEntryLine = new Regex("(\\d+) (.+)");

    public IEnumerable<DecklistEntry> ReadDecklist(TextReader tr)
    {
        string? line = tr.ReadLine();
        bool isSideboard = false;
        while (line != null)
        {
            // A line containing the word "sideboard" should be sufficient indicator that the sideboard
            // section is about to begin...
            //
            // ...Until wotc stupidly prints a new card with that word in it!
            if (line.Contains("sideboard", StringComparison.OrdinalIgnoreCase))
            {
                isSideboard = true;
                line = tr.ReadLine();
                continue;
            }

            if (TryParseLine(line, isSideboard, out var ent))
            {
                yield return ent;
            }

            line = tr.ReadLine();
        }

        bool TryParseLine(string line, bool isSideboard, out DecklistEntry entry)
        {
            entry = new DecklistEntry(0, string.Empty, isSideboard);

            // A quantity entry should be of the form:
            //
            //   * n CARDNAME
            //   * nx CARDNAME
            //
            // Which this regex should catch
            var m = smEntryLine.Match(line);
            if (m.Groups.Count == 3)
            {
                // First group should be the quantity
                if (int.TryParse(m.Groups[1].ValueSpan, out var qty))
                {
                    entry = entry with { Quantity = qty, CardName = m.Groups[2].Value.Trim() };
                    return true;
                }
            }

            return false;
        }
    }
}
