using System;
using System.Collections.Generic;

namespace MtgCollectionTracker.Core.Model;

/// <summary>A single price data point (price + vendor) within a date's snapshot.</summary>
public class CardPricePoint
{
    public double Price { get; set; }

    public required string Provider { get; set; }
}

/// <summary>Low/median/high price snapshot for a single date.</summary>
public class CardPriceDatePoint
{
    public DateOnly Date { get; set; }

    /// <summary>Lowest retail price on this date.</summary>
    public CardPricePoint? Lowest { get; set; }

    /// <summary>Highest retail price on this date.</summary>
    public CardPricePoint? Highest { get; set; }

    /// <summary>Median retail price on this date.</summary>
    public double? Median { get; set; }
}

/// <summary>Price history for a card SKU across up to 10 recent dates.</summary>
public class CardPriceHistoryModel
{
    public required string CardName { get; set; }

    public required string Edition { get; set; }

    public bool IsFoil { get; set; }

    /// <summary>Date points ordered ascending (oldest first, most recent last).</summary>
    public List<CardPriceDatePoint> DatePoints { get; set; } = new();
}
