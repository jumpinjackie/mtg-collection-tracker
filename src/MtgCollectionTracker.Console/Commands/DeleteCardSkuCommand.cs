using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using MtgCollectionTracker.Core.Services;

namespace MtgCollectionTracker.Console.Commands;

[Verb("delete-card-sku", HelpText = "Delete a card sku from your collection. This is for if you entered a sku by mistake or you sold or traded away this quantity of cards")]
internal class DeleteCardSkuCommand : CommandBase
{
    [Option("sku-id", Required = true, HelpText = "The id of the card sku")]
    public int SkuId { get; set; }

    protected override async ValueTask<int> ExecuteInternalAsync(IServiceProvider serviceProvider)
    {
        var service = serviceProvider.GetRequiredService<CollectionTrackingService>();
        var res = await service.DeleteCardSkuAsync(this.SkuId);
        if (res != null)
        {
            Stdout($"Card sku {res.GetDescription(false)} deleted");
        }
        return 0;
    }
}
