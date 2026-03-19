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

    /// <summary>
    /// Indicates whether this is a Commander deck
    /// </summary>
    public bool IsCommander { get; set; }

    /// <summary>
    /// The name of the commander card, if this is a Commander deck
    /// </summary>
    public string? CommanderName { get; set; }

    /// <summary>
    /// The validation status of the commander deck (null if not a commander deck or validation not yet run)
    /// </summary>
    public bool? IsCommanderValid { get; set; }
}
