using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services.Stubs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MtgCollectionTracker.ViewModels;

/// <summary>One colored bar within a date group of the price history chart.</summary>
public partial class PriceBarViewModel : ObservableObject
{
    /// <summary>Fractional height of this bar relative to the tallest bar (0.0–1.0).</summary>
    [ObservableProperty]
    private double _heightRatio;

    /// <summary>Solid color brush name for this bar (e.g. "Green", "Gold", "Crimson").</summary>
    public required string Color { get; init; }

    /// <summary>Text shown in the tooltip when hovering over this bar.</summary>
    public required string Tooltip { get; init; }

    /// <summary>Formatted price label (e.g. "$1.23").</summary>
    public required string PriceLabel { get; init; }
}

/// <summary>One date column in the price history chart, containing low/median/high bars.</summary>
public class PriceBarGroupViewModel
{
    public required string DateLabel { get; init; }

    public PriceBarViewModel? LowBar { get; init; }

    public PriceBarViewModel? MedianBar { get; init; }

    public PriceBarViewModel? HighBar { get; init; }
}

/// <summary>ViewModel for the "Price History" dialog showing a bar chart of price data across up to 10 dates.</summary>
public partial class CardPriceHistoryViewModel : DialogContentViewModel
{
    private const string DefaultCurrency = "USD";
    private const double ChartMaxHeight = 200.0;

    readonly ICollectionTrackingService _service;

    public CardPriceHistoryViewModel()
        : base()
    {
        _service = new StubCollectionTrackingService();
        CardName = "Sample Card";
        Edition = "TST";
        NoDataMessage = "No price history available.";
        HasNoData = true;
    }

    public CardPriceHistoryViewModel(ICollectionTrackingService service, IMessenger messenger)
        : base(messenger)
    {
        _service = service;
    }

    [ObservableProperty]
    private string _cardName = string.Empty;

    [ObservableProperty]
    private string _edition = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _hasNoData;

    [ObservableProperty]
    private string _noDataMessage = string.Empty;

    /// <summary>Y-axis tick labels (price values) from top to bottom.</summary>
    public ObservableCollection<string> YAxisLabels { get; } = new();

    /// <summary>Bar groups for the chart, one per date in ascending order.</summary>
    public ObservableCollection<PriceBarGroupViewModel> BarGroups { get; } = new();

    public async Task LoadAsync(Guid skuId, CancellationToken cancel = default)
    {
        IsBusy = true;
        try
        {
            var history = await _service.GetPriceHistoryForSkuAsync(skuId, DefaultCurrency, cancel);
            if (history == null || history.DatePoints.Count == 0)
            {
                HasNoData = true;
                NoDataMessage = "No price history available for this card.";
                return;
            }

            CardName = history.CardName;
            Edition = history.Edition;

            BuildChart(history);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void BuildChart(CardPriceHistoryModel history)
    {
        // Determine the max price across all dates and all metrics for scaling
        double maxPrice = history.DatePoints
            .SelectMany(dp => new[]
            {
                dp.Lowest?.Price ?? 0.0,
                dp.Highest?.Price ?? 0.0,
                dp.Median ?? 0.0
            })
            .DefaultIfEmpty(0.0)
            .Max();

        if (maxPrice <= 0.0)
        {
            HasNoData = true;
            NoDataMessage = "No valid price data found for this card.";
            return;
        }

        // Build Y-axis labels (5 evenly spaced ticks from max to 0)
        YAxisLabels.Clear();
        for (int i = 4; i >= 0; i--)
        {
            double labelValue = maxPrice * i / 4.0;
            YAxisLabels.Add($"${labelValue:F2}");
        }

        // Build bar groups
        BarGroups.Clear();
        foreach (var dp in history.DatePoints)
        {
            var group = new PriceBarGroupViewModel
            {
                DateLabel = dp.Date.ToString("dd MMM yy"),
                LowBar = dp.Lowest != null ? new PriceBarViewModel
                {
                    Color = "#4CAF50",
                    HeightRatio = dp.Lowest.Price / maxPrice,
                    PriceLabel = $"${dp.Lowest.Price:F2}",
                    Tooltip = $"Lowest: ${dp.Lowest.Price:F2} ({dp.Lowest.Provider})"
                } : null,
                MedianBar = dp.Median.HasValue ? new PriceBarViewModel
                {
                    Color = "#FFC107",
                    HeightRatio = dp.Median.Value / maxPrice,
                    PriceLabel = $"${dp.Median.Value:F2}",
                    Tooltip = $"Median: ${dp.Median.Value:F2}"
                } : null,
                HighBar = dp.Highest != null ? new PriceBarViewModel
                {
                    Color = "#F44336",
                    HeightRatio = dp.Highest.Price / maxPrice,
                    PriceLabel = $"${dp.Highest.Price:F2}",
                    Tooltip = $"Highest: ${dp.Highest.Price:F2} ({dp.Highest.Provider})"
                } : null
            };
            BarGroups.Add(group);
        }
    }
}
