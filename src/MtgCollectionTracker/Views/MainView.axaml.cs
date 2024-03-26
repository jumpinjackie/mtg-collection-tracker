using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using MtgCollectionTracker.ViewModels;

namespace MtgCollectionTracker.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
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
