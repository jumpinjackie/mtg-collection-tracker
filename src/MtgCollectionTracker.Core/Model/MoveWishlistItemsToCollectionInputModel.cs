namespace MtgCollectionTracker.Core.Model;

public class MoveWishlistItemsToCollectionInputModel
{
    public int? ContainerId { get; set; }

    public required int[] WishlistItemIds { get; init; }
}
