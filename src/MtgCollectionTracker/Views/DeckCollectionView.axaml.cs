using Avalonia.Controls;
using Avalonia.Interactivity;
using MtgCollectionTracker.ViewModels;

namespace MtgCollectionTracker.Views;

public partial class DeckCollectionView : UserControl
{
    public DeckCollectionView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (this.DataContext is DeckCollectionViewModel vm)
        {
            vm.LoadDecks();
        }
    }
}