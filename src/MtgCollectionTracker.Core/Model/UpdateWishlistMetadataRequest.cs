namespace MtgCollectionTracker.Core.Model;

/// <summary>Request body for triggering a wishlist-metadata update operation.</summary>
public record UpdateWishlistMetadataRequest(ICollection<int> Ids);
