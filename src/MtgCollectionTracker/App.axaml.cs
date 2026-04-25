using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using MtgCollectionTracker.Data;
using MtgCollectionTracker.Services.Messaging;
using MtgCollectionTracker.ViewModels;
using MtgCollectionTracker.Views;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MtgCollectionTracker;

public partial class App : Application
{
    /// <summary>
    /// Optional hook set by the host (e.g. <c>Program.cs</c>) that is called with the chosen
    /// <see cref="AppSettings"/> after the user confirms their mode selection.  Use this to
    /// start platform-specific services (such as the embedded sharing server) before the main
    /// window opens.
    /// </summary>
    public static Action<AppSettings>? AfterModeSelected { get; set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Install global unhandled exception handlers so errors are shown in a dialog
        // rather than crashing the app.
        Dispatcher.UIThread.UnhandledException += OnUiThreadUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var startupVm = new StartupModeViewModel();
            var startupWindow = new StartupWindow { DataContext = startupVm };

            startupVm.LaunchRequested += (_, settings) =>
            {
                AfterModeSelected?.Invoke(settings);

                var mainWindow = new MainWindow { WindowState = Avalonia.Controls.WindowState.Maximized };
                var cnt = new Container(mainWindow);

                if (!Avalonia.Controls.Design.IsDesignMode && cnt.Mode != AppMode.RemoteClient)
                {
                    using var db = new CardsDbContext(cnt.CreateDbContextOptions());
                    db.Database.Migrate();
                }

                mainWindow.DataContext = cnt.Resolve().Value;
                desktop.MainWindow = mainWindow;
                mainWindow.Show();
                startupWindow.Close();
            };

            desktop.MainWindow = startupWindow;
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            var root = new MainView();
            var cnt = new Container(root);

            if (!Avalonia.Controls.Design.IsDesignMode && cnt.Mode != AppMode.RemoteClient)
            {
                using var db = new CardsDbContext(cnt.CreateDbContextOptions());
                db.Database.Migrate();
            }

            root.DataContext = cnt.Resolve().Value;
            singleViewPlatform.MainView = root;
        }
        else
        {
            throw new InvalidOperationException("Unsupported application lifetime.");
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void OnUiThreadUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        if (IsIgnorableUnhandledException(e.Exception))
            return;

        ShowExceptionDialog(e.Exception);
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        e.SetObserved();
        var ex = e.Exception.InnerException ?? e.Exception;
        if (IsIgnorableUnhandledException(ex))
            return;

        Dispatcher.UIThread.Post(() => ShowExceptionDialog(ex));
    }

    // 0 = no dialog open, 1 = dialog already open.  Use Interlocked for thread-safe compare-and-swap.
    private static int _exceptionDialogVisible;

    private static void ShowExceptionDialog(Exception ex)
    {
        // Only ever show one error dialog at a time – drop subsequent exceptions while one is open.
        if (Interlocked.CompareExchange(ref _exceptionDialogVisible, 1, 0) != 0)
            return;

        var errorVm = new UnhandledExceptionViewModel
        {
            Message = ex.Message,
            Details = ex.ToString(),
            OnClosed = () => Interlocked.Exchange(ref _exceptionDialogVisible, 0)
        };
        var dialogVm = new DialogViewModel();
        dialogVm.WithContent("An error occurred", errorVm);
        WeakReferenceMessenger.Default.Send(new OpenDialogMessage { ViewModel = dialogVm });
    }

    private static bool IsIgnorableUnhandledException(Exception ex)
    {
        for (Exception? current = ex; current != null; current = current.InnerException)
        {
            if (current.GetType().FullName == "Tmds.DBus.Protocol.DBusException"
                && current.Message.Contains("org.freedesktop.DBus.Error.ServiceUnknown", StringComparison.Ordinal)
                && current.Message.Contains("The name is not activatable", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
