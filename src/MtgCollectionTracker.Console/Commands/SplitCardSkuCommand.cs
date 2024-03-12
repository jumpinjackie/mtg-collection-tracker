using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using MtgCollectionTracker.Core.Services;

namespace MtgCollectionTracker.Console.Commands;

[Verb("split-card-sku", HelpText = "Split a quantity of cards in a sku off to a separate sku. The sku must not be already in a deck")]
internal class SplitCardSkuCommand : CommandBase
{
    [Option("card-sku-id", HelpText = "The card sku to split")]
    public int CardSkuId { get; set; }

    [Option("quantity", HelpText = "The quantity to split off. This must be greater than 0 and less than the quantity of the sku to split")]
    public int Quantity { get; set; }

    protected override async ValueTask<int> ExecuteInternalAsync(IServiceProvider serviceProvider)
    {
        var service = serviceProvider.GetRequiredService<CollectionTrackingService>();
        var res = await service.SplitCardSkuAsync(new()
        {
            CardSkuId = this.CardSkuId,
            Quantity = this.Quantity
        });
        Stdout($"{res.Quantity}x of ({res.CardName}, {res.Edition}) split off into separate new sku (id: {res.Id}) in container: {res.ContainerName ?? "<un-assigned>"}");
        return 0;
    }
}
