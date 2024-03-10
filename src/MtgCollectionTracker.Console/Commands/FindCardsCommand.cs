using CommandLine;
using ConsoleTables;
using Microsoft.Extensions.DependencyInjection;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;

namespace MtgCollectionTracker.Console.Commands;

[Verb("find-cards")]
internal class FindCardsCommand : CommandBase
{
    [Option("name", Required = true)]
    public required string Name { get; set; }

    [Option("not-in-decks", Required = true)]
    public bool NotInDecks { get; set; }

    protected override async ValueTask<int> ExecuteInternalAsync(IServiceProvider serviceProvider)
    {
        var service = serviceProvider.GetRequiredService<CollectionTrackingService>();
        var matches = service.GetCards(new() { SearchFilter = this.Name });

        var table = new ConsoleTable(
            nameof(CardSkuModel.Id),
            nameof(CardSkuModel.CardName),
            nameof(CardSkuModel.Edition),
            nameof(CardSkuModel.Language),
            nameof(CardSkuModel.DeckName), 
            nameof(CardSkuModel.ContainerName));

        foreach (var m in matches)
        {
            table.AddRow(m.Id, m.CardName, m.Edition, m.Language, m.DeckName, m.ContainerName);
        }

        table.Write();

        return 0;
    }
}
