using System;
using System.IO;

using Avalonia;

using MtgCollectionTracker;
using MtgCollectionTracker.Server;

namespace MtgCollectionTracker.Desktop;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        var settings = AppSettings.Load();
        EmbeddedServerHost? serverHost = null;

        if (settings.Mode == AppMode.Server)
        {
            var dbPath = settings.DbPath != null
                ? Path.GetFullPath(settings.DbPath)
                : Path.GetFullPath("collection.sqlite");
            serverHost = EmbeddedServerHost.Start(settings.ServerPort, settings.HostApiKey, dbPath);
        }

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            serverHost?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}

