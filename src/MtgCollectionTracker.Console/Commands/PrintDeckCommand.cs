using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using MtgCollectionTracker.Core.Services;

namespace MtgCollectionTracker.Console.Commands;

[Verb("print-deck", HelpText = "Prints a given deck")]
internal class PrintDeckCommand : CommandBase
{
    [Option("deck-id", Required = true, HelpText = "The id of the deck to print")]
    public int DeckId { get; set; }

    [Option("report-proxy-usage", Required = false, HelpText = "If true, will print proxy instances and stats as part of the decklist")]
    public bool ReportProxyUsage { get; set; }

    protected override async ValueTask<int> ExecuteInternalAsync(IServiceProvider serviceProvider)
    {
        var service = serviceProvider.GetRequiredService<CollectionTrackingService>();

        var text = service.PrintDeck(this.DeckId, new DeckPrintOptions(this.ReportProxyUsage));
        Stdout(text);

        return 0;
    }
}
