namespace MtgCollectionTracker.Core.Model;

public record struct MoveWishlistItemQuantityInputModel(int WishlistItemId, int Quantity);

public class MoveWishlistItemsToCollectionInputModel
{
    public int? ContainerId { get; set; }

    public required MoveWishlistItemQuantityInputModel[] Items { get; init; }
}
