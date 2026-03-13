using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MtgCollectionTracker.ViewModels;

/// <summary>
/// Represents a named counter placed on a card or token during playtesting
/// </summary>
public partial class CardCounterViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayColor))]
    private string _counterName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayColor), nameof(LabelForeground))]
    private Color _counterColor = Colors.LimeGreen;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CounterTooltipText))]
    private int _quantity = 1;

    /// <summary>
    /// Short text shown in tooltips: "CounterName: Qty"
    /// </summary>
    public string CounterTooltipText => $"{CounterName}: {Quantity}";

    /// <summary>
    /// The IBrush for the counter background
    /// </summary>
    public IBrush DisplayColor => new SolidColorBrush(CounterColor);

    /// <summary>
    /// Contrasting foreground color for readability on the counter circle
    /// </summary>
    public IBrush LabelForeground
    {
        get
        {
            // Relative luminance: use white on dark backgrounds, black on light backgrounds
            var c = CounterColor;
            var luminance = (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) / 255.0;
            return luminance > 0.5 ? Brushes.Black : Brushes.White;
        }
    }
}
