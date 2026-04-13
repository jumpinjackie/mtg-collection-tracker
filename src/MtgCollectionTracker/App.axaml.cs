using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using MtgCollectionTracker.Data;
using MtgCollectionTracker.Views;
using System;

namespace MtgCollectionTracker;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Action<Container>? init = null;
        Visual? root = null;
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            root = new MainWindow() { WindowState = Avalonia.Controls.WindowState.Maximized };
            init = (cnt) =>
            {
                root.DataContext = cnt.Resolve().Value;
                desktop.MainWindow = (MainWindow)root;
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            root = new MainView();
            init = cnt =>
            {
                root.DataContext = cnt.Resolve().Value;
                singleViewPlatform.MainView = (MainView)root;
            };
        }
        if (root is null || init is null)
        {
            throw new InvalidOperationException("Unsupported application lifetime.");
        }

        var cnt = new Container(root);
        if (!Avalonia.Controls.Design.IsDesignMode)
        {
            using (var db = new CardsDbContext(cnt.CreateDbContextOptions()))
            {
                //Stdout("Creating database and applying migrations if required");
                db.Database.Migrate();
            }
        }

        init?.Invoke(cnt);

        base.OnFrameworkInitializationCompleted();
    }
}
