using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MtgCollectionTracker.Console.Commands;

[Verb("update-card-sku", HelpText = "Update various aspects of a card sku")]
internal class UpdateCardSkuCommand : CommandBase
{
    [Option("card-sku-id", Required = false, HelpText = "The id of the card sku to update")]
    public int CardSkuId { get; set; }

    [Option("quantity", Required = false, HelpText = "The new quantity to apply to this sku")]
    public int? Quantity { get; set; }

    [Option("edition", Required = false, HelpText = "The edition to apply to this sku")]
    public string? Edition { get; set; }

    [Option("lang", Required = false, HelpText = "The language to apply to this sku")]
    public string? Language { get; set; }

    [Option("comments", Required = false, HelpText = "The comments to apply to this sku")]
    public string? Comments { get; set; }

    [Option("condition", Required = false, HelpText = "The condition to apply to this sku")]
    public CardCondition? Condition { get; set; }

    [Option("deck-id", Required = false, HelpText = "The id of the deck to associate this sku to")]
    public int? DeckId { get; set; }

    [Option("container-id", Required = false, HelpText = "The id of the container to associate this sku to")]
    public int? ContainerId { get; set; }

    [Option("unset-deck", Required = false, Default = false, HelpText = "If true, will un-set the associated deck for this sku. Nullifies --deck-id if both specified")]
    public bool UnsetDeck { get; set; }

    [Option("unset-container", Required = false, Default = false, HelpText = "If true, will un-set the associated container for this sku. Nullifies --container-id if both specified")]
    public bool UnsetContainer { get; set; }

    protected override async ValueTask<int> ExecuteInternalAsync(IServiceProvider serviceProvider)
    {
        var service = serviceProvider.GetRequiredService<CollectionTrackingService>();
        var res = await service.UpdateCardSkuAsync(new()
        {
            Id = this.CardSkuId,
            Quantity = this.Quantity,
            Comments = this.Comments,
            Condition = this.Condition,
            Edition = this.Edition,
            Language = this.Language,
            DeckId = this.DeckId,
            ContainerId = this.ContainerId,
            UnsetDeck = this.UnsetDeck,
            UnsetContainer = this.UnsetContainer
        });
        Stdout($"Updated sku [{res.Quantity}x {res.CardName}, id: {res.Id}]");
        if (res.DeckName == null && res.ContainerName == null)
        {
            Stdout("Sku is now not part of any deck or container");
        }
        else
        {
            if (res.DeckName != null)
                Stdout($"Sku is now part of deck: {res.DeckName}");
            if (res.ContainerName != null)
                Stdout($"Sku is now part of container: {res.ContainerName}");
        }
        return 0;
    }
}
