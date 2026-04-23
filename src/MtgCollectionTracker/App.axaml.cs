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
        ShowExceptionDialog(e.Exception);
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        e.SetObserved();
        var ex = e.Exception.InnerException ?? e.Exception;
        Dispatcher.UIThread.Post(() => ShowExceptionDialog(ex));
    }

    private static void ShowExceptionDialog(Exception ex)
    {
        var errorVm = new UnhandledExceptionViewModel
        {
            Message = ex.Message,
            Details = ex.ToString()
        };
        var dialogVm = new DialogViewModel();
        dialogVm.WithContent("An error occurred", errorVm);
        WeakReferenceMessenger.Default.Send(new OpenDialogMessage { ViewModel = dialogVm });
    }
}
