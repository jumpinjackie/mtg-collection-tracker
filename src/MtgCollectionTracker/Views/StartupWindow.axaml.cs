using Avalonia.Controls;
using MtgCollectionTracker.ViewModels;
using System;

namespace MtgCollectionTracker.Views;

public partial class StartupWindow : Window
{
    public StartupWindow()
    {
        InitializeComponent();
    }

    /// <summary>Fired when the user clicks Launch in the startup view.</summary>
    public event EventHandler<AppSettings>? LaunchRequested;

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is StartupModeViewModel vm)
            vm.LaunchRequested += OnLaunchRequested;
    }

    private void OnLaunchRequested(object? sender, AppSettings settings)
    {
        LaunchRequested?.Invoke(this, settings);
    }
}
