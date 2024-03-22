using Avalonia.Controls;
using Avalonia.Interactivity;
using MtgCollectionTracker.ViewModels;

namespace MtgCollectionTracker.Views;

public partial class ContainerSetView : UserControl
{
    public ContainerSetView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (this.DataContext is ContainerSetViewModel vm)
        {
            vm.LoadContainers();
        }
    }
}