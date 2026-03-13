using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using MtgCollectionTracker.ViewModels;

namespace MtgCollectionTracker.Views;

public partial class MulliganSelectionView : UserControl
{
    private PlaytestCardViewModel? _lastHoveredCard;

    public MulliganSelectionView()
    {
        InitializeComponent();
    }

    private void OnCardBorderPointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is Border border && border.DataContext is PlaytestCardViewModel card)
        {
            if (this.DataContext is MulliganSelectionViewModel vm)
            {
                _lastHoveredCard = card;
                vm.SelectedCard = card;
            }
        }
    }

    private void OnCardListSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var card = e.AddedItems.OfType<PlaytestCardViewModel>().LastOrDefault();
        if (card is null)
        {
            return;
        }

        if (this.DataContext is MulliganSelectionViewModel vm)
        {
            _lastHoveredCard = card;
            vm.SelectedCard = card;
        }
    }

    private void OnCardListPointerExited(object? sender, PointerEventArgs e)
    {
        if (_lastHoveredCard is null)
        {
            return;
        }

        if (this.DataContext is MulliganSelectionViewModel vm)
        {
            vm.SelectedCard = _lastHoveredCard;
        }
    }
}
