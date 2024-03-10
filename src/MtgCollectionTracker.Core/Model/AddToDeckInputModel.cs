namespace MtgCollectionTracker.Core.Model;

public class AddToDeckInputModel
{
    public int CardSkuId { get; set; }

    public int DeckId { get; set; }

    public int Quantity { get; set; }

    public bool IsSideboard { get; set; }
}
