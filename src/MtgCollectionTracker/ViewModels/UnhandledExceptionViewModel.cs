using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Services.Messaging;

namespace MtgCollectionTracker.ViewModels;

public partial class UnhandledExceptionViewModel : DialogContentViewModel
{
    public string Message { get; init; } = string.Empty;

    public string Details { get; init; } = string.Empty;

    [RelayCommand]
    private void Close()
    {
        Messenger.Send(new CloseDialogMessage());
    }

    [RelayCommand]
    private static void Quit()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown(1);
        else
            System.Environment.Exit(1);
    }
}
