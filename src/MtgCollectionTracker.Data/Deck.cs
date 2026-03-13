using System.ComponentModel.DataAnnotations;

namespace MtgCollectionTracker.Data;

/// <summary>
/// Defines a Magic: The Gathering deck
/// 
/// It may optionally be part of a <see cref="Container"/>
/// </summary>
public class Deck
{
    public int Id { get; set; }

    /// <summary>
    /// The name of this deck
    /// </summary>
    [MaxLength(64)]
    public required string Name { get; set; }

    /// <summary>
    /// The format this deck is legal in or intended to be played in
    /// </summary>
    [MaxLength(32)]
    public string? Format { get; set; }

    public int? ContainerId { get; set; }

    /// <summary>
    /// The optional container this deck is part of
    /// </summary>
    public virtual Container? Container { get; set; }

    /// <summary>
    /// The cards in this deck
    /// </summary>
    public virtual ICollection<CardSku> Cards { get; set; }

    /// <summary>
    /// The optional banner card SKU id for this deck. When set, the card image of this SKU
    /// is used as the visual banner of this deck in the deck box view.
    /// </summary>
    public Guid? BannerCardId { get; set; }

    /// <summary>
    /// The optional banner card SKU for this deck
    /// </summary>
    public virtual CardSku? BannerCard { get; set; }
}
