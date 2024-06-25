using System.ComponentModel.DataAnnotations;

namespace MtgCollectionTracker.Data;

/// <summary>
/// A <see cref="WishlistItem"/> defines a specific quantity of a particular card with price data
/// from various vendors
/// </summary>
public class WishlistItem : IScryfallMetaLinkable
{
    public int Id { get; set; }

    /// <summary>
    /// The quantity of cards
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// The name of the card
    /// </summary>
    [MaxLength(256)]
    public required string CardName { get; set; }

    /// <summary>
    /// The code for the card's edition
    /// </summary>
    [MaxLength(5)]
    public required string Edition { get; set; }

    /// <summary>
    /// The languages of this quantity of cards. If not specified, it is assumed to be in English
    /// </summary>
    [MaxLength(3)]
    public string? LanguageId { get; set; }

    public virtual CardLanguage? Language { get; set; }

    /// <summary>
    /// The collector number
    /// </summary>
    [MaxLength(5)]
    public string? CollectorNumber { get; set; }

    /// <summary>
    /// The scryfall card id, used to associate with auxilliary information like card image, market price, etc
    /// </summary>
    public string? ScryfallId { get; set; }

    public virtual ScryfallCardMetadata? Scryfall { get; set; }

    /// <summary>
    /// Indicates if this quantity of cards is foil
    /// </summary>
    public bool IsFoil { get; set; }

    /// <summary>
    /// Indicates the condition of this quantity of cards. If not specified it assumed to be
    /// <see cref="CardCondition.NearMint"/>
    /// </summary>
    public CardCondition? Condition { get; set; }

    /// <summary>
    /// Indicates if this card is a land. This is to assist in deck stats and decklist generation/presentation
    /// </summary>
    public bool IsLand { get; set; }

    /// <summary>
    /// Prices for this item offered by various vendors
    /// </summary>
    public virtual ICollection<VendorPrice> OfferedPrices { get; set; }

    /// <summary>
    /// The best (cheapest) offer among the prices offered by various vendors
    /// </summary>
    public VendorPrice? BestOffer => OfferedPrices?.OrderBy(op => op.Price).FirstOrDefault();

    /// <summary>
    /// Creates a new SKU for this wishlist item
    /// </summary>
    /// <param name="containerId"></param>
    /// <returns></returns>
    public CardSku CreateSku(int? containerId)
    {
        return new CardSku
        {
            CardName = this.CardName,
            Edition = this.Edition,
            CollectorNumber = this.CollectorNumber,
            Quantity = this.Quantity,
            IsLand = this.IsLand,
            ScryfallId = this.ScryfallId,
            Condition = this.Condition,
            IsFoil = this.IsFoil,
            LanguageId = this.LanguageId,
            ContainerId = containerId
        };
    }
}
