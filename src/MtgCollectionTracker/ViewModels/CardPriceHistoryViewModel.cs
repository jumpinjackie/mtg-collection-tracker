using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services.Stubs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

    /// <summary>Pixel height used by the chart UI for this bar.</summary>
    [ObservableProperty]
    private double _pixelHeight;

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

/// <summary>One Y-axis tick label with both text and resolved pixel position.</summary>
public partial class YAxisLabelViewModel : ObservableObject
{
    /// <summary>Formatted tick label text (e.g. "$3.75").</summary>
    public required string Text { get; init; }

    /// <summary>Value ratio in the range 0.0-1.0 where 1.0 is top of the plot.</summary>
    public required double HeightRatio { get; init; }

    /// <summary>Top pixel coordinate inside the plot area.</summary>
    [ObservableProperty]
    private double _pixelTop;
}

/// <summary>ViewModel for the "Price History" dialog showing a bar chart of price data across up to 10 dates.</summary>
public partial class CardPriceHistoryViewModel : DialogContentViewModel
{
    private const string DefaultCurrency = "USD";

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

    [ObservableProperty]
    private double _chartPlotHeight = 200.0;

    // ── Card-details panel (shown to the right of the chart) ────────────────

    private ICardSkuItem? _cardSku;
    private INotifyPropertyChanged? _cardSkuNotifier;

    /// <summary>Whether the card-details panel should be shown (i.e. a card sku was supplied).</summary>
    [ObservableProperty]
    private bool _hasCardDetails;

    /// <summary>Large card image, delegated to the supplied card sku.</summary>
    public Task<Bitmap?> CardImageLarge => _cardSku?.CardImageLarge ?? Task.FromResult<Bitmap?>(null);

    /// <summary>Whether the large card image is currently loading.</summary>
    public bool IsCardImageLargeLoading => _cardSku?.IsCardImageLargeLoading ?? false;

    /// <summary>Whether the card has a back face.</summary>
    public bool IsDoubleFaced => _cardSku?.IsDoubleFaced ?? false;

    /// <summary>Label for the face-switch button (e.g. "Switch to Back").</summary>
    public string? SwitchLabel => _cardSku?.SwitchLabel;

    /// <summary>Command that flips to the other face of a double-faced card.</summary>
    public IRelayCommand? SwitchFaceCommand => _cardSku?.SwitchFaceCommand;

    /// <summary>Mana cost string for the card.</summary>
    public string? CastingCost => _cardSku?.CastingCost;

    /// <summary>Oracle text for the card.</summary>
    public string? OracleText => _cardSku?.OracleText;

    /// <summary>Type line of the card.</summary>
    public string? CardType => _cardSku?.CardType;

    /// <summary>Power/toughness string (e.g. "3/3") or null if not applicable.</summary>
    public string? PT => _cardSku?.PT;

    /// <summary>
    /// Attaches card-sku data so the card-details panel can display the image,
    /// oracle text and other metadata alongside the pricing chart.
    /// </summary>
    public CardPriceHistoryViewModel WithCardSku(ICardSkuItem sku)
    {
        if (_cardSkuNotifier != null)
        {
            _cardSkuNotifier.PropertyChanged -= OnCardSkuPropertyChanged;
            _cardSkuNotifier = null;
        }

        _cardSku = sku;
        HasCardDetails = true;

        if (sku is INotifyPropertyChanged notifier)
        {
            _cardSkuNotifier = notifier;
            _cardSkuNotifier.PropertyChanged += OnCardSkuPropertyChanged;
        }

        OnPropertyChanged(nameof(CardImageLarge));
        OnPropertyChanged(nameof(IsCardImageLargeLoading));
        OnPropertyChanged(nameof(IsDoubleFaced));
        OnPropertyChanged(nameof(SwitchLabel));
        OnPropertyChanged(nameof(SwitchFaceCommand));
        OnPropertyChanged(nameof(CastingCost));
        OnPropertyChanged(nameof(OracleText));
        OnPropertyChanged(nameof(CardType));
        OnPropertyChanged(nameof(PT));

        return this;
    }

    private void OnCardSkuPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ICardSkuItem.IsCardImageLargeLoading):
                OnPropertyChanged(nameof(IsCardImageLargeLoading));
                break;
            case nameof(ICardSkuItem.CardImageLarge):
                OnPropertyChanged(nameof(CardImageLarge));
                break;
            case nameof(ICardSkuItem.IsDoubleFaced):
                OnPropertyChanged(nameof(IsDoubleFaced));
                break;
            case nameof(ICardSkuItem.SwitchLabel):
                OnPropertyChanged(nameof(SwitchLabel));
                break;
        }
    }

    // ────────────────────────────────────────────────────────────────────────

    /// <summary>Y-axis tick labels (price values) from top to bottom.</summary>
    public ObservableCollection<YAxisLabelViewModel> YAxisLabels { get; } = new();

    /// <summary>Bar groups for the chart, one per date in ascending order.</summary>
    public ObservableCollection<PriceBarGroupViewModel> BarGroups { get; } = new();

    partial void OnChartPlotHeightChanged(double value)
    {
        UpdateYAxisPixelPositions();
        UpdateBarPixelHeights();
    }

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

        HasNoData = false;
        NoDataMessage = string.Empty;

        // Build Y-axis labels (5 evenly spaced ticks from max to 0)
        YAxisLabels.Clear();
        for (int i = 4; i >= 0; i--)
        {
            double ratio = i / 4.0;
            double labelValue = maxPrice * i / 4.0;
            YAxisLabels.Add(new YAxisLabelViewModel
            {
                Text = $"${labelValue:F2}",
                HeightRatio = ratio,
            });
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

        UpdateYAxisPixelPositions();
        UpdateBarPixelHeights();
    }

    private void UpdateYAxisPixelPositions()
    {
        double plotHeight = Math.Max(1.0, ChartPlotHeight);
        const double labelOffset = 7.0;

        foreach (var label in YAxisLabels)
        {
            double pixelTop = ((1.0 - label.HeightRatio) * plotHeight) - labelOffset;
            label.PixelTop = Math.Clamp(pixelTop, 0.0, plotHeight - 12.0);
        }
    }

    private void UpdateBarPixelHeights()
    {
        double plotHeight = Math.Max(1.0, ChartPlotHeight);
        foreach (var group in BarGroups)
        {
            SetBarPixelHeight(group.LowBar, plotHeight);
            SetBarPixelHeight(group.MedianBar, plotHeight);
            SetBarPixelHeight(group.HighBar, plotHeight);
        }
    }

    private static void SetBarPixelHeight(PriceBarViewModel? bar, double plotHeight)
    {
        if (bar == null)
        {
            return;
        }

        bar.PixelHeight = Math.Max(1.0, bar.HeightRatio * plotHeight);
    }
}
