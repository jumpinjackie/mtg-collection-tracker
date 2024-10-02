using MtgCollectionTracker.Data;

namespace MtgCollectionTracker.Core.Model;

public class VendorOfferModel : IVendorOffer
{
    public int VendorId { get; set; }

    public required string VendorName { get; set; }

    public int AvailableStock { get; set; }

    public decimal Price { get; set; }

    public string? Notes { get; set; }

    string IVendorOffer.Name => VendorName;
}

public class WishlistItemModel
{
    public int Id { get; set; }

    public string? ScryfallId { get; set; }

    public int Quantity { get; set; }

    public required string CardName { get; set; }

    public required string Edition { get; set; }

    public string? Language { get; set; }

    public string? CollectorNumber { get; set; }

    public bool IsFoil { get; set; }

    public CardCondition? Condition { get; set; }

    public bool IsLand { get; set; }

    public bool IsDoubleFaced { get; set; }

    public required List<VendorOfferModel> Offers { get; set; }

    public string[] Tags { get; set; }

    public string? CastingCost { get; set; }

    public string? OracleText { get; set; }

    public string? CardType { get; set; }

    public string? Power { get; set; }

    public string? Toughness { get; set; }

    public string[]? Colors { get; set; }

    public string[]? ColorIdentity { get; set; }
}
