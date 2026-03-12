using System.Linq;
using Avalonia;
using Avalonia.Controls;
using MtgCollectionTracker.ViewModels;

namespace MtgCollectionTracker.Views;

public partial class ZoneContentsView : UserControl
{
    public ZoneContentsView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Keeps <see cref="RootPanel"/>'s MaxHeight equal to the UserControl's actual arranged
    /// height so that the DockPanel's inner Grid * row receives the correct finite constraint
    /// and the button row is always pinned to the bottom regardless of window size.
    /// </summary>
    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        if (e.NewSize.Height > 0 && !double.IsInfinity(e.NewSize.Height))
        {
            RootPanel.MaxHeight = e.NewSize.Height;
        }
    }

    private void OnCardListSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var card = e.AddedItems.OfType<PlaytestCardViewModel>().LastOrDefault();
        if (card is null)
        {
            return;
        }

        if (DataContext is ZoneContentsViewModel vm)
        {
            vm.SelectedCard = card;
        }
    }
}
