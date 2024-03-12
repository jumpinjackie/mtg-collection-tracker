using CommandLine;
using ConsoleTables;
using Microsoft.Extensions.DependencyInjection;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;

namespace MtgCollectionTracker.Console.Commands;

[Verb("find-cards", HelpText = "Finds cards in your collection with the given name")]
internal class FindCardsCommand : CommandBase
{
    [Option("name", Required = true, HelpText = "The name of the card. Name can be partial.")]
    public required string Name { get; set; }

    [Option("not-in-decks", Required = false, HelpText = "If true, will omit card skus from results where it is already in another deck")]
    public bool NotInDecks { get; set; }

    [Option("no-proxies", Required = false, HelpText = "If true, will omit card skus from results where it is a proxy")]
    public bool NoProxies { get; set; }

    protected override async ValueTask<int> ExecuteInternalAsync(IServiceProvider serviceProvider)
    {
        var service = serviceProvider.GetRequiredService<CollectionTrackingService>();
        var matches = service.GetCards(new() { SearchFilter = this.Name, NotInDecks = this.NotInDecks, NoProxies = this.NoProxies });

        var table = new ConsoleTable(
            nameof(CardSkuModel.Id),
            nameof(CardSkuModel.Quantity),
            nameof(CardSkuModel.CardName),
            nameof(CardSkuModel.Edition),
            nameof(CardSkuModel.Language),
            nameof(CardSkuModel.DeckName), 
            nameof(CardSkuModel.ContainerName),
            nameof(CardSkuModel.Comments));

        foreach (var m in matches)
        {
            table.AddRow(m.Id, m.Quantity, m.CardName, m.Edition, m.Language, m.DeckName, m.ContainerName, m.Comments);
        }

        table.Write();

        return 0;
    }
}
