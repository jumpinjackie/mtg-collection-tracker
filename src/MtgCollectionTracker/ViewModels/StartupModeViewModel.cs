using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MtgCollectionTracker.ViewModels;

public partial class StartupModeViewModel : ObservableObject
{
    public StartupModeViewModel()
    {
        // Load last-used settings as defaults so the user's previous choice is pre-selected.
        var settings = AppSettings.Load();
        _mode = settings.Mode;
        _remoteServerUrl = settings.RemoteServerUrl;
        _remoteApiKey = settings.RemoteApiKey;
        _serverPort = settings.ServerPort;
        _hostApiKey = settings.HostApiKey;
    }

    /// <summary>Fired when the user clicks Launch; the argument carries the saved settings.</summary>
    public event EventHandler<AppSettings>? LaunchRequested;

    /// <summary>All available modes shown in the mode picker.</summary>
    public AppMode[] Modes { get; } = [AppMode.Local, AppMode.Server, AppMode.RemoteClient];

    // ── Mode ──────────────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRemoteClientMode))]
    [NotifyPropertyChangedFor(nameof(IsServerMode))]
    [NotifyPropertyChangedFor(nameof(ModeDescription))]
    [NotifyCanExecuteChangedFor(nameof(TestConnectionCommand))]
    private AppMode _mode = AppMode.Local;

    public bool IsRemoteClientMode => Mode == AppMode.RemoteClient;

    public bool IsServerMode => Mode == AppMode.Server;

    /// <summary>A human-readable description of the currently selected <see cref="Mode"/>.</summary>
    public string ModeDescription => Mode switch
    {
        AppMode.Local =>
            "Use the local collection database on this machine. " +
            "This is the default mode for a single-machine setup.",
        AppMode.Server =>
            "Start an embedded sharing server so that other devices on your local network " +
            "can connect to this machine's collection database using Remote Client mode.",
        AppMode.RemoteClient =>
            "Connect to a remote MTG Collection Tracker instance running in Server mode. " +
            "All operations are forwarded to that server.",
        _ => string.Empty,
    };

    // ── Remote client settings ────────────────────────────────────────────────

    [ObservableProperty]
    private string _remoteServerUrl = "http://localhost:5757";

    [ObservableProperty]
    private string? _remoteApiKey;

    // ── Embedded-server settings ──────────────────────────────────────────────

    [ObservableProperty]
    private int _serverPort = 5757;

    [ObservableProperty]
    private string? _hostApiKey;

    // ── Connection test ───────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(TestConnectionCommand))]
    private bool _isTestingConnection;

    [ObservableProperty]
    private string? _connectionTestResult;

    [RelayCommand(CanExecute = nameof(CanTestConnection))]
    private async Task TestConnection(CancellationToken cancel)
    {
        IsTestingConnection = true;
        ConnectionTestResult = null;
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var url = RemoteServerUrl?.TrimEnd('/') ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(RemoteApiKey))
                http.DefaultRequestHeaders.Add("X-Api-Key", RemoteApiKey);
            var resp = await http.GetAsync($"{url}/api/health", cancel);
            ConnectionTestResult = resp.IsSuccessStatusCode
                ? "✓ Connection successful"
                : $"✗ Server returned {(int)resp.StatusCode}";
        }
        catch (Exception ex)
        {
            ConnectionTestResult = $"✗ {ex.Message}";
        }
        finally
        {
            IsTestingConnection = false;
        }
    }

    private bool CanTestConnection() => !IsTestingConnection && IsRemoteClientMode;

    // ── Launch ────────────────────────────────────────────────────────────────

    [RelayCommand]
    private void Launch()
    {
        var settings = AppSettings.Load();
        settings.Mode = Mode;
        settings.RemoteServerUrl = RemoteServerUrl;
        settings.RemoteApiKey = RemoteApiKey;
        settings.ServerPort = ServerPort;
        settings.HostApiKey = HostApiKey;
        settings.Save();

        LaunchRequested?.Invoke(this, settings);
    }
}
