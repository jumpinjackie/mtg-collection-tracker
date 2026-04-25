using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Services;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MtgCollectionTracker.ViewModels;

public partial class ConnectionSettingsViewModel : ObservableObject
{
    private readonly IMessenger _messenger;

    public ConnectionSettingsViewModel(IMessenger messenger)
    {
        _messenger = messenger;
        var settings = AppSettings.Load();
        _mode = settings.Mode;
        _remoteServerUrl = settings.RemoteServerUrl;
        _remoteApiKey = settings.RemoteApiKey;
        _serverPort = settings.ServerPort;
        _hostApiKey = settings.HostApiKey;
    }

    // Design-time constructor
    public ConnectionSettingsViewModel()
    {
        _messenger = WeakReferenceMessenger.Default;
    }

    /// <summary>All available modes shown in the mode picker.</summary>
    public AppMode[] Modes { get; } = [AppMode.Local, AppMode.Server, AppMode.RemoteClient];

    // ── Mode ──────────────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRemoteClientMode))]
    [NotifyPropertyChangedFor(nameof(IsServerMode))]
    private AppMode _mode = AppMode.Local;

    public bool IsRemoteClientMode => Mode == AppMode.RemoteClient;

    public bool IsServerMode => Mode == AppMode.Server;

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
            if (resp.IsSuccessStatusCode)
            {
                ConnectionTestResult = "✓ Connection successful";
                _messenger.ToastNotify("Connection test passed", Avalonia.Controls.Notifications.NotificationType.Success);
            }
            else
            {
                ConnectionTestResult = $"✗ Server returned {(int)resp.StatusCode}";
            }
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

    // ── Save ──────────────────────────────────────────────────────────────────

    [RelayCommand]
    private void Save()
    {
        var settings = AppSettings.Load();
        settings.Mode = Mode;
        settings.RemoteServerUrl = RemoteServerUrl;
        settings.RemoteApiKey = RemoteApiKey;
        settings.ServerPort = ServerPort;
        settings.HostApiKey = HostApiKey;
        settings.Save();

        _messenger.ToastNotify("Connection settings saved — restart the application to apply changes.", Avalonia.Controls.Notifications.NotificationType.Information);
    }
}
