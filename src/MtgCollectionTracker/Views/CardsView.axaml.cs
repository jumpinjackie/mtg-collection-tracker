using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using MtgCollectionTracker.ViewModels;

namespace MtgCollectionTracker.Views;

public partial class CardsView : UserControl
{
    public CardsView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (this.DataContext is CardsViewModel vm)
        {
            vm.Load();
        }
    }

    private void OnSearchKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && this.DataContext is CardsViewModel vm)
        {
            vm.PerformSearchCommand.Execute(null);
        }
    }
}