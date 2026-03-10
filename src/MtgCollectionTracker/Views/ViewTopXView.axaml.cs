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

    private void OnCardListSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var card = e.AddedItems.OfType<PlaytestCardViewModel>().LastOrDefault();
        if (card is null)
            return;

        if (DataContext is ViewTopXViewModel vm)
            vm.SelectedCard = card;
    }
}
