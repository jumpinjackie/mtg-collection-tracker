using CommandLine;
using ConsoleTables;
using Microsoft.Extensions.DependencyInjection;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;

namespace MtgCollectionTracker.Console.Commands;

[Verb("list-decks")]
internal class ListDecksCommand : CommandBase
{
    protected override async ValueTask<int> ExecuteInternalAsync(IServiceProvider serviceProvider)
    {
        var service = serviceProvider.GetRequiredService<CollectionTrackingService>();

        var decks = service.GetDecks();
        var table = new ConsoleTable(
            nameof(DeckSummaryModel.Id),
            nameof(DeckSummaryModel.Name),
            nameof(DeckSummaryModel.ContainerName),
            nameof(DeckSummaryModel.MaindeckTotal),
            nameof(DeckSummaryModel.SideboardTotal));

        foreach (var m in decks)
        {
            table.AddRow(m.Id, m.Name, m.ContainerName, m.MaindeckTotal, m.SideboardTotal);
        }

        table.Write();

        return 0;
    }
}
