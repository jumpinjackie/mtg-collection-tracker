namespace MtgCollectionTracker.Services.Messaging;

internal class CardsAddedMessage
{
    public int CardsTotal { get; init; }

    public int ProxyTotal { get; init; }

    public int SkuTotal { get; init; }
}
