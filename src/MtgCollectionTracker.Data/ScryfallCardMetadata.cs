using System.ComponentModel.DataAnnotations;

namespace MtgCollectionTracker.Data;

public class ScryfallCardMetadata
{
    /// <summary>
    /// Scryfall card object id
    /// </summary>
    [MaxLength(36)]
    public required string Id { get; set; }

    /// <summary>
    /// Card name
    /// </summary>
    [MaxLength(256)]
    public required string CardName { get; set; }

    /// <summary>
    /// The code for the card's edition
    /// </summary>
    [MaxLength(5)]
    public required string Edition { get; set; }

    /// <summary>
    /// The card's typeline
    /// </summary>
    [MaxLength(128)]
    public required string CardType { get; set; }

    /// <summary>
    /// The card type, parsed from the card's typeline
    /// </summary>
    [MaxLength(64)]
    public string? Type { get; set; }

    /// <summary>
    /// The card's mana value (aka. converted mana cost)
    /// </summary>
    public int? ManaValue { get; set; }

    /// <summary>
    /// The card's oracle text
    /// </summary>
    [MaxLength(650)] // The current world record holder (Dance of the Dead) is 641 characters
    public string? OracleText { get; set; }

    /// <summary>
    /// The card's colors
    /// </summary>
    public string[]? Colors { get; set; }

    /// <summary>
    /// The card's color identity (for commander purposes)
    /// </summary>
    public string[]? ColorIdentity { get; set; }

    /// <summary>
    /// The card's power (for creatures)
    /// </summary>
    public string? Power { get; set; }

    /// <summary>
    /// The card's toughness (for creatures)
    /// </summary>
    public string? Toughness { get; set; }

    /// <summary>
    /// The card's casting cost
    /// </summary>
    [MaxLength(32)]
    public string? CastingCost { get; set; }

    [MaxLength(11)]
    public required string Rarity { get; set; }

    [MaxLength(5)]
    public string? CollectorNumber { get; set; }

    /// <summary>
    /// The languages of this quantity of cards. If not specified, it is assumed to be in English
    /// </summary>
    [MaxLength(3)]
    public string? Language { get; set; }

    /// <summary>
    /// Small card image URL (JPG format)
    /// </summary>
    public string? ImageSmallUrl { get; set; }

    /// <summary>
    /// Small back face card image URL (JPG format)
    /// </summary>
    public string? BackImageSmallUrl { get; set; }

    /// <summary>
    /// Large card image URL (JPG format)
    /// </summary>
    public string? ImageLargeUrl { get; set; }

    /// <summary>
    /// Large back face card image URL (JPG format)
    /// </summary>
    public string? BackImageLargeUrl { get; set; }
}
