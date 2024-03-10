namespace MtgCollectionTracker.Core.Model;

public class RemoveFromDeckInputModel
{
    public int CardSkuId { get; set; }

    public int Quantity { get; set; }

    /// <summary>
    /// The id of the container to return this quantity to
    /// </summary>
    public int ContainerId { get; set; }
}
