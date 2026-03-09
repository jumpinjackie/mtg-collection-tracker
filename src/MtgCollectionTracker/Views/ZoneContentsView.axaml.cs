using System.Linq;
using Avalonia.Controls;
using MtgCollectionTracker.ViewModels;

namespace MtgCollectionTracker.Views;

public partial class ZoneContentsView : UserControl
{
    public ZoneContentsView()
    {
        InitializeComponent();
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
