using System.Linq;
using Avalonia;
using Avalonia.Controls;
using MtgCollectionTracker.ViewModels;

namespace MtgCollectionTracker.Views;

public partial class ViewTopXView : UserControl
{
    public ViewTopXView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Keeps <see cref="RootGrid"/>'s MaxHeight equal to the UserControl's actual arranged
    /// height. DialogHostAvalonia measures popup content with infinite height, which causes
    /// Grid * rows to behave like Auto rows and push button rows off-screen. By updating
    /// MaxHeight to the real arranged height each layout cycle the * row receives the correct
    /// finite constraint and the button row is always pinned to the bottom.
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
        if (DataContext is not ViewTopXViewModel vm)
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
