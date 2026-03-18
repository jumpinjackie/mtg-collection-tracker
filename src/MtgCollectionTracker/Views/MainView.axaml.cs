using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Input;
using Avalonia.Interactivity;
using MtgCollectionTracker.ViewModels;

namespace MtgCollectionTracker.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        AddHandler(KeyDownEvent, OnMainViewKeyDown, RoutingStrategies.Tunnel);
    }

    private void OnMainViewKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && this.DataContext is MainViewModel vm && vm.TryHandleEscapeForDialog())
        {
            e.Handled = true;
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (this.DataContext is MainViewModel vm)
        {
            // NOTE: WindowNotificationManager doesn't seem to work on WASM (browser) environment
            var nm = new WindowNotificationManager(TopLevel.GetTopLevel(this)!);
            nm.Position = NotificationPosition.BottomRight;
            vm.NotificationManager = nm;
        }
    }
}
