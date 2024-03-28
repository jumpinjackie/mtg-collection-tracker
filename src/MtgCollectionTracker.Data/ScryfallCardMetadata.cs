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

    [MaxLength(128)]
    public required string CardType { get; set; }

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
    /// Large card image (JPG format)
    /// </summary>
    public byte[]? ImageLarge { get; set; }

    /// <summary>
    /// Small card image (JPG format)
    /// </summary>
    public byte[]? ImageSmall { get; set; }
}
