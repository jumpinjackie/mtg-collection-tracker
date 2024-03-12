using MtgCollectionTracker.Data;

namespace MtgCollectionTracker.Core.Model;

public class UpdateCardSkuInputModel
{
    public int Id { get; set; }

    public int? Quantity { get; set; }

    public string? Edition { get; set; }

    public string? Language { get; set; }

    public string? Comments { get; set; }

    public CardCondition? Condition { get; set; }

    public int? DeckId { get; set; }

    public int? ContainerId { get; set; }

    public bool UnsetDeck { get; set; }

    public bool UnsetContainer { get; set; }
}
