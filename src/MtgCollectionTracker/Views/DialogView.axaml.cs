using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using MtgCollectionTracker.ViewModels;

namespace MtgCollectionTracker.Views;

public partial class DialogView : UserControl
{
    public DialogView()
    {
        InitializeComponent();
        AddHandler(KeyDownEvent, OnDialogKeyDown, RoutingStrategies.Tunnel);
    }

    private void OnDialogKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && DataContext is DialogViewModel vm && !vm.CanClose)
        {
            e.Handled = true;
        }
    }
}
