using System;

namespace MtgCollectionTracker.Core.Model;

public class DeckSummaryModel : DeckModelBase
{
    public int MaindeckTotal { get; set; }

    public int SideboardTotal { get; set; }

    /// <summary>
    /// The Scryfall ID of the banner card SKU, if one has been designated for this deck
    /// </summary>
    public string? BannerScryfallId { get; set; }

    /// <summary>
    /// The ID of the banner card SKU, if one has been designated for this deck
    /// </summary>
    public Guid? BannerCardId { get; set; }
}
