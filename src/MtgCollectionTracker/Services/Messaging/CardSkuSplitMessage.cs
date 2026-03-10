using System;

namespace MtgCollectionTracker.Services.Messaging;

internal class CardSkuSplitMessage
{
    public int? ContainerId { get; set; }

    public int? DeckId { get; set; }

    public required int Quantity { get; set; }

    public required Guid SplitSkuId { get; set; }

    public required Guid NewSkuId { get; set; }
}
