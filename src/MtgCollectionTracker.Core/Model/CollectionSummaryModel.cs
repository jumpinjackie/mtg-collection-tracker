namespace MtgCollectionTracker.Core.Model;

public class CollectionSummaryModel
{
    public int CardTotal { get; init; }

    public int ProxyTotal { get; init; }

    public int SkuTotal { get; init; }

    public int DeckTotal { get; init; }

    public int ContainerTotal { get; set; }

    public static CollectionSummaryModel Empty()
    {
        return new CollectionSummaryModel
        {
            CardTotal = 0,
            ProxyTotal = 0,
            SkuTotal = 0,
            DeckTotal = 0,
            ContainerTotal = 0
        };
    }
}
