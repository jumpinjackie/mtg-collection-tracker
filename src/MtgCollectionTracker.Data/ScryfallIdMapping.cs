using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MtgCollectionTracker.Data;

/// <summary>
/// Maps a Scryfall card ID to its MTG JSON UUID. Populated by importing the MTG JSON cardIdentifiers.csv feed.
/// </summary>
public class ScryfallIdMapping
{
    [MaxLength(36)]
    public required string ScryfallId { get; set; }

    [MaxLength(36)]
    public required string MtgJsonUuid { get; set; }

    public virtual ScryfallCardMetadata? ScryfallCardMetadata { get; set; }

    public virtual ICollection<CardPricingEntry> CardPricingEntries { get; set; } = new List<CardPricingEntry>();
}
