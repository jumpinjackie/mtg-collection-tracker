using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Data;

namespace MtgCollectionTracker.Console.Commands;

[Verb("add-card-sku", HelpText = "Add new card sku to a container or deck in your collection")]
internal class AddCardSkuCommand : CommandBase
{
    [Option("quantity", Required = true, HelpText = "The quantity of the card")]
    public int Quantity { get; set; }

    [Option("card-name", Required = true, HelpText = "The name of the card")]
    public required string CardName { get; set; }

    [Option("edition", Required = true, HelpText = "The edition of the card")]
    public required string Edition { get; set; }

    [Option("container-id", Required = false, HelpText = "The id of the container")]
    public int? ContainerId { get; set; }

    [Option("deck-id", Required = false, HelpText = "The id of the deck")]
    public int? DeckId { get; set; }

    [Option("is-land", Required = false, Default = false, HelpText = "If true, indicates this card is a land")]
    public bool IsLand { get; set; }

    [Option("is-sideboard", Required = false, Default = false, HelpText = "If true, indicates this card is for sideboard")]
    public bool IsSideboard { get; set; }

    [Option("is-foil", Required = false, Default = false, HelpText = "If true, indicates this card is foil")]
    public bool IsFoil { get; set; }

    [Option("condition", Required = false)]
    public CardCondition? Condition { get; set; }

    [Option("comments")]
    public string? Comments { get; set; }

    protected override async ValueTask<int> ExecuteInternalAsync(IServiceProvider serviceProvider)
    {
        var service = serviceProvider.GetRequiredService<CollectionTrackingService>();

        var res = await service.AddToDeckOrContainerAsync(this.ContainerId, this.DeckId, new()
        {
            Quantity = this.Quantity,
            CardName = this.CardName,
            Edition = this.Edition,
            IsLand = this.IsLand,
            IsSideboard = this.IsSideboard,
            IsFoil = this.IsFoil,
            Condition = this.Condition,
            Comments = this.Comments
        });

        Stdout($"Added {res.GetDescription()} to [{res.DeckName ?? res.ContainerName ?? "<un-assigned>"}]");

        return 0;
    }
}
