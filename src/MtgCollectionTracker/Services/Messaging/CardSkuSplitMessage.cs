namespace MtgCollectionTracker.Services.Messaging;

internal class CardSkuSplitMessage
{
    public int? ContainerId { get; set; }

    public int? DeckId { get; set; }

    public required int SplitSkuId { get; set; }

    public required int NewSkuId { get; set; }
}
