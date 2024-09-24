using CommandLine;
using ConsoleTables;
using Microsoft.Extensions.DependencyInjection;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;

namespace MtgCollectionTracker.Console.Commands;

[Verb("list-decks", HelpText = "Lists all decks in your collection")]
internal class ListDecksCommand : CommandBase
{
    [Option("format", Required = false, HelpText = "Filter decks for the given format")]
    public string? Format { get; set; }

    protected override async ValueTask<int> ExecuteInternalAsync(IServiceProvider serviceProvider)
    {
        var service = serviceProvider.GetRequiredService<CollectionTrackingService>();

        var decks = service.GetDecks(new () { Formats = string.IsNullOrEmpty(this.Format) ? [] : [this.Format] });
        var table = new ConsoleTable(
            nameof(DeckSummaryModel.Id),
            nameof(DeckSummaryModel.Name),
            nameof(DeckSummaryModel.Format),
            nameof(DeckSummaryModel.ContainerName),
            nameof(DeckSummaryModel.MaindeckTotal),
            nameof(DeckSummaryModel.SideboardTotal));

        foreach (var m in decks)
        {
            table.AddRow(m.Id, m.Name, m.Format, m.ContainerName, m.MaindeckTotal, m.SideboardTotal);
        }

        table.Write();

        return 0;
    }
}
