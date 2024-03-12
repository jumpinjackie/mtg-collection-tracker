using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using MtgCollectionTracker.Core.Services;

namespace MtgCollectionTracker.Console.Commands;

[Verb("dismantle-deck", HelpText = "Removes the given deck and returns all assigned card skus to a given container")]
internal class DismantleDeckCommand : CommandBase
{
    [Option("deck-id", HelpText = "The id of the deck to dismantle")]
    public int DeckId { get; set; }

    [Option("container-id", HelpText = "The option id of the container to return these cards to. Otherwise it will be returned to an 'unassigned' pool")]
    public int? ContainerId { get; set; }

    protected override async ValueTask<int> ExecuteInternalAsync(IServiceProvider serviceProvider)
    {
        var service = serviceProvider.GetRequiredService<CollectionTrackingService>();

        var res = await service.DismantleDeckAsync(new()
        {
            DeckId = this.DeckId,
            ContainerId = this.ContainerId
        });

        Stdout($"{res.Removed} cards removed from deck and returned to container ({res.ContainerName ?? "<un-assigned>"})");

        return 0;
    }
}
