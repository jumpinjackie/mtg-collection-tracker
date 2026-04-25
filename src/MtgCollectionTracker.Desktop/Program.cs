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
        EmbeddedServerHost? serverHost = null;

        // Register a hook that starts the embedded server (if needed) after the user
        // confirms their mode selection in the startup window.
        App.AfterModeSelected = settings =>
        {
            if (settings.Mode == AppMode.Server)
            {
                var dbPath = settings.DbPath != null
                    ? Path.GetFullPath(settings.DbPath)
                    : Path.GetFullPath("collection.sqlite");
                serverHost = EmbeddedServerHost.Start(settings.ServerPort, settings.HostApiKey, dbPath);
            }
        };

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            serverHost?.Dispose();
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
