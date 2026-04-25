using System;
using System.IO;
using System.Text.Json;

namespace MtgCollectionTracker;

/// <summary>Controls how this instance accesses the collection database.</summary>
public enum AppMode
{
    /// <summary>Read the local collection.sqlite file directly (default).</summary>
    Local,

    /// <summary>
    /// Start an embedded Kestrel server so that other app instances on the local
    /// network can connect to this machine's collection database.
    /// </summary>
    Server,

    /// <summary>Forward all operations to a remote <c>MtgCollectionTracker.Server</c> instance.</summary>
    RemoteClient,
}

/// <summary>Application-level settings persisted to disk in the OS app-data folder.</summary>
public class AppSettings
{
    private static readonly JsonSerializerOptions s_jsonOptions =
        new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    // ── Shared ────────────────────────────────────────────────────────────────

    /// <summary>Which service mode to use on next startup.</summary>
    public AppMode Mode { get; set; } = AppMode.Local;

    // ── Remote-client settings ────────────────────────────────────────────────

    /// <summary>Base URL of the remote server, e.g. <c>http://192.168.1.10:5757</c>.</summary>
    public string RemoteServerUrl { get; set; } = "http://localhost:5757";

    /// <summary>API key sent with every request to the remote server.</summary>
    public string? RemoteApiKey { get; set; }

    // ── Server / host settings ────────────────────────────────────────────────

    /// <summary>TCP port on which the embedded Kestrel server listens.</summary>
    public int ServerPort { get; set; } = 5757;

    /// <summary>API key that clients must supply.  Leave empty to disable authentication.</summary>
    public string? HostApiKey { get; set; }

    // ── Storage ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Override the path to <c>collection.sqlite</c>.
    /// When <see langword="null"/> the default <c>collection.sqlite</c> in the working
    /// directory is used (preserving backward-compatible behaviour).
    /// </summary>
    public string? DbPath { get; set; }

    // ── Persistence ───────────────────────────────────────────────────────────

    private static string GetSettingsFilePath()
    {
        var appDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "mtg-collection-tracker");
        Directory.CreateDirectory(appDir);
        return Path.Combine(appDir, "appsettings.json");
    }

    /// <summary>Load settings from disk, returning defaults if the file is absent or corrupt.</summary>
    public static AppSettings Load()
    {
        var path = GetSettingsFilePath();
        if (!File.Exists(path))
            return new AppSettings();
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<AppSettings>(json, s_jsonOptions) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    /// <summary>Persist the current settings to disk.</summary>
    public void Save()
    {
        var path = GetSettingsFilePath();
        var json = JsonSerializer.Serialize(this, s_jsonOptions);
        File.WriteAllText(path, json);
    }
}
