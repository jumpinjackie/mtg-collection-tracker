using MtgCollectionTracker.Data;

namespace MtgCollectionTracker.Core.Model;

public class UpdateVendorOfferInputModel
{
    public int VendorId { get; set; }

    public decimal Price { get; set; } 

    public int Available { get; set; }

    public string? Notes { get; set; }
}

public class UpdateWishlistItemInputModel
{
    public int Id { get; set; }

    public string? CardName { get; set; }

    public int? Quantity { get; set; }

    public string? Edition { get; set; }

    public string? Language { get; set; }

    public string? CollectorNumber { get; set; }

    public bool? IsFoil { get; set; }

    public CardCondition? Condition { get; set; }

    public IEnumerable<UpdateVendorOfferInputModel>? VendorOffers { get; set; }
}
