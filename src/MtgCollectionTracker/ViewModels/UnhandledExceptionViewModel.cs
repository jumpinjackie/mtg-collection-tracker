using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Services.Messaging;
using System;

namespace MtgCollectionTracker.ViewModels;

public partial class UnhandledExceptionViewModel : DialogContentViewModel
{
    public string Message { get; init; } = string.Empty;

    public string Details { get; init; } = string.Empty;

    /// <summary>
    /// Called when the dialog is dismissed (via Close or Quit) so the caller can reset
    /// any "dialog already open" guard.
    /// </summary>
    public Action? OnClosed { get; init; }

    [RelayCommand]
    private void Close()
    {
        OnClosed?.Invoke();
        Messenger.Send(new CloseDialogMessage());
    }

    [RelayCommand]
    private void Quit()
    {
        OnClosed?.Invoke();
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown(1);
        else
            System.Environment.Exit(1);
    }
}
