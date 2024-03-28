using Avalonia.Controls;
using Avalonia.Input;
using MtgCollectionTracker.ViewModels;

namespace MtgCollectionTracker.Views;

public partial class CardsView : UserControl
{
    public CardsView()
    {
        InitializeComponent();
    }

    private void OnSearchKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && this.DataContext is CardsViewModel vm)
        {
            vm.PerformSearchCommand.Execute(null);
        }
    }
}