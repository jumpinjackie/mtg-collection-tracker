using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using MtgCollectionTracker.Data;
using MtgCollectionTracker.ViewModels;
using MtgCollectionTracker.Views;
using System;

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
}
