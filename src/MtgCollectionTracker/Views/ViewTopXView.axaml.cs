using System.Linq;
using Avalonia.Controls;
using MtgCollectionTracker.ViewModels;

namespace MtgCollectionTracker.Views;

public partial class ViewTopXView : UserControl
{
    public ViewTopXView()
    {
        InitializeComponent();
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
