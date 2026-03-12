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
    /// Keeps <see cref="RootGrid"/>'s MaxHeight equal to the UserControl's actual arranged
    /// height so that the Grid * row receives the correct finite constraint and the button row
    /// is always pinned to the bottom regardless of window size.
    /// </summary>
    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        if (e.NewSize.Height > 0 && !double.IsInfinity(e.NewSize.Height))
        {
            RootGrid.MaxHeight = e.NewSize.Height;
        }
    }

    private void OnCardDataGridSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not ZoneContentsViewModel vm)
            return;

        foreach (var item in e.AddedItems.OfType<PlaytestCardViewModel>())
        {
            if (!vm.SelectedCards.Contains(item))
                vm.SelectedCards.Add(item);
        }

        foreach (var item in e.RemovedItems.OfType<PlaytestCardViewModel>())
        {
            vm.SelectedCards.Remove(item);
        }

        var lastAdded = e.AddedItems.OfType<PlaytestCardViewModel>().LastOrDefault();
        if (lastAdded is not null)
            vm.SelectedCard = lastAdded;
    }
}
