namespace MtgCollectionTracker.Core.Model;

public record WishlistItemMoveResult(int WishlistItemId, CardSkuModel Card);

public class MoveWishlistItemsToCollectionResult
{
    public required WishlistItemMoveResult[] CreatedSkus { get; init; }
}
