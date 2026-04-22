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

    private StartupModeViewModel? _previousVm;

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_previousVm != null)
        {
            _previousVm.LaunchRequested -= OnLaunchRequested;
            _previousVm = null;
        }

        if (DataContext is StartupModeViewModel vm)
        {
            vm.LaunchRequested += OnLaunchRequested;
            _previousVm = vm;
        }
    }

    private void OnLaunchRequested(object? sender, AppSettings settings)
    {
        LaunchRequested?.Invoke(this, settings);
    }
}
