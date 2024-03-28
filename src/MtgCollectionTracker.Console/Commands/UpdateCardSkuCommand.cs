using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Data;

namespace MtgCollectionTracker.Console.Commands;

[Verb("update-card-sku", HelpText = "Update various aspects of one or more card skus. Be careful what you apply when updating multiple skus")]
internal class UpdateCardSkuCommand : CommandBase
{
    [Option("card-sku-ids", Required = true, HelpText = "The ids of the card skus to update")]
    public required IEnumerable<int> CardSkuIds { get; set; }

    [Option("quantity", Required = false, HelpText = "The new quantity to apply to affected skus")]
    public int? Quantity { get; set; }

    [Option("edition", Required = false, HelpText = "The edition to apply to affected skus")]
    public string? Edition { get; set; }

    [Option("lang", Required = false, HelpText = "The language to apply to affected skus")]
    public string? Language { get; set; }

    [Option("comments", Required = false, HelpText = "The comments to apply to affected skus")]
    public string? Comments { get; set; }

    [Option("condition", Required = false, HelpText = "The condition to apply to affected skus")]
    public CardCondition? Condition { get; set; }

    [Option("deck-id", Required = false, HelpText = "The id of the deck to associate affected skus to")]
    public int? DeckId { get; set; }

    [Option("container-id", Required = false, HelpText = "The id of the container to associate affected skus to")]
    public int? ContainerId { get; set; }

    [Option("is-sideboard", Required = false, HelpText = "Mark all affected skus to be for sideboard")]
    public bool IsSideboard { get; set; }

    [Option("is-land", Required = false, HelpText = "Mark all affected skus as land")]
    public bool IsLand { get; set; }

    [Option("unset-deck", Required = false, Default = false, HelpText = "If true, will un-set the associated deck for affected skus. Nullifies --deck-id if both specified")]
    public bool UnsetDeck { get; set; }

    [Option("unset-container", Required = false, Default = false, HelpText = "If true, will un-set the associated container for affected skus. Nullifies --container-id if both specified")]
    public bool UnsetContainer { get; set; }

    protected override async ValueTask<int> ExecuteInternalAsync(IServiceProvider serviceProvider)
    {
        bool? sb = null;
        bool? land = null;
        if (IsSideboard)
            sb = true;
        if (IsLand)
            land = true;

        var service = serviceProvider.GetRequiredService<CollectionTrackingService>();
        var res = await service.UpdateCardSkuAsync(new()
        {
            Ids = this.CardSkuIds,
            Quantity = this.Quantity,
            Comments = this.Comments,
            Condition = this.Condition,
            Edition = this.Edition,
            Language = this.Language,
            DeckId = this.DeckId,
            IsSideboard = sb,
            IsLand = land,
            ContainerId = this.ContainerId,
            UnsetDeck = this.UnsetDeck,
            UnsetContainer = this.UnsetContainer
        }, null, CancellationToken.None);
        Stdout($"{res} skus updated");
        return 0;
    }
}
