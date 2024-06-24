namespace MtgCollectionTracker.Data;

/// <summary>
/// Defines a price that a <see cref="Vendor"/> can offer for a given card, along with the quantity of
/// that card they have in stock
/// </summary>
public class VendorPrice : IVendorOffer
{
    public int Id { get; set; }

    public int ItemId { get; set; }

    /// <summary>
    /// The card you are after
    /// </summary>
    public virtual WishlistItem Item { get; set; }

    public int VendorId { get; set; }

    /// <summary>
    /// The vendor offering the card
    /// </summary>
    public virtual Vendor Vendor { get; set; }

    /// <summary>
    /// The price offered by this vendor 
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// The quantity of this card they have in stock
    /// </summary>
    public int AvailableStock { get; set; }

    string IVendorOffer.Name => Vendor.Name;

    int IVendorOffer.AvailableStock => AvailableStock;

    decimal IVendorOffer.Price => Price;
}
