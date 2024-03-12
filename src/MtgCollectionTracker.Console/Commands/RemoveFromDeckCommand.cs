using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using MtgCollectionTracker.Core.Services;

namespace MtgCollectionTracker.Console.Commands;

[Verb("remove-from-deck", HelpText = "Removes a card sku from an existing deck")]
internal class RemoveFromDeckCommand : CommandBase
{
    [Option("card-sku-id", HelpText = "The id of card sku to remove")]
    public int CardSkuId { get; set; }

    [Option("container-id", HelpText = "The id of the container to return this sku to")]
    public int? ContainerId { get; set; }

    [Option("quantity", HelpText = "The quantity to remove")]
    public int Quantity { get; set; }

    protected override async ValueTask<int> ExecuteInternalAsync(IServiceProvider serviceProvider)
    {
        var service = serviceProvider.GetRequiredService<CollectionTrackingService>();
        var (res, wasMerged) = await service.RemoveFromDeckAsync(new()
        {
            CardSkuId = this.CardSkuId,
            ContainerId = this.ContainerId,
            Quantity = this.Quantity
        });
        Stdout($"{this.Quantity} cards removed from deck and returned to container: {res.ContainerName ?? "<un-assigned>"}");
        if (wasMerged)
            Stdout($"This amount has been merged into sku {res.GetDescription()}");
        else
            Stdout($"A new sku {res.GetDescription()} has been created");
        return 0;
    }
}
