using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using MtgCollectionTracker.Core.Services;

namespace MtgCollectionTracker.Console.Commands;

[Verb("consolidate-skus", HelpText = "Consolidates identical skus (with same name/edition/language/condition/comments) in a given deck or container. If deck or container is not specified, consolidation will happen in the un-assigned container")]
internal class ConsolidateSkusCommand : CommandBase
{
    [Option("deck-id", Required = false, HelpText = "The id of the deck to consolidate skus in")]
    public int? DeckId { get; set; }

    [Option("container-id", Required = false, HelpText = "The id of the container to consolidate skus in")]
    public int? ContainerId { get; set; }

    protected override async ValueTask<int> ExecuteInternalAsync(IServiceProvider serviceProvider)
    {
        var service = serviceProvider.GetRequiredService<CollectionTrackingService>();
        var (updated, removed) = await service.ConsolidateCardSkusAsync(new()
        {
            DeckId = this.DeckId,
            ContainerId = this.ContainerId
        });
        Stdout($"{removed} skus removed and their quantities merged into {updated} skus");
        return 0;
    }
}
