using System.ComponentModel.DataAnnotations;

namespace MtgCollectionTracker.Data;

/// <summary>
/// A <see cref="Vendor"/> is an entity that has quantity of your <see cref="WishlistItem"/> for a given price
/// </summary>
public class Vendor
{
    public int Id { get; set; }

    /// <summary>
    /// The name of the vendor
    /// </summary>
    [MaxLength(200)]
    public string Name { get; set; }

    /// <summary>
    /// All prices offered by this vendor
    /// </summary>
    public virtual ICollection<VendorPrice> OfferedPrices { get; set; }
}
