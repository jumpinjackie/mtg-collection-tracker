using CommandLine;
using ConsoleTables;
using Microsoft.Extensions.DependencyInjection;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;

namespace MtgCollectionTracker.Console.Commands;

[Verb("list-containers")]
internal class ListContainersCommand : CommandBase
{
    protected override async ValueTask<int> ExecuteInternalAsync(IServiceProvider serviceProvider)
    {
        var service = serviceProvider.GetRequiredService<CollectionTrackingService>();

        var decks = service.GetContainers();
        var table = new ConsoleTable(
            nameof(ContainerSummaryModel.Id),
            nameof(ContainerSummaryModel.Name),
            nameof(ContainerSummaryModel.Total));

        foreach (var m in decks)
        {
            table.AddRow(m.Id, m.Name, m.Total);
        }

        table.Write();

        return 0;
    }
}
