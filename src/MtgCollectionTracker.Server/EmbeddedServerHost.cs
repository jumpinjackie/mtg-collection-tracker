using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace MtgCollectionTracker.Server;

/// <summary>
/// Manages an out-of-process <c>MtgCollectionTracker.Server</c> instance started
/// by the Desktop application when running in <c>Server</c> mode.
/// </summary>
public sealed class EmbeddedServerHost : IDisposable, IAsyncDisposable
{
    private readonly Process? _process;

    private EmbeddedServerHost(Process? process) => _process = process;

    /// <summary>
    /// Locate and start the server executable alongside this binary, returning a handle
    /// that stops the process when disposed.
    /// </summary>
    /// <param name="port">TCP port to listen on.</param>
    /// <param name="apiKey">Optional shared API key.</param>
    /// <param name="dbPath">Absolute path to the SQLite database file.</param>
    public static EmbeddedServerHost Start(int port, string? apiKey, string dbPath)
    {
        var exe = FindServerExecutable();
        if (exe is null)
            return new EmbeddedServerHost(null);

        var psi = new ProcessStartInfo
        {
            FileName = exe,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        // Configure via double-underscore environment-variable overrides
        // (standard ASP.NET Core configuration provider convention).
        psi.Environment["Server__Port"] = port.ToString();
        psi.Environment["Server__DbPath"] = dbPath;
        if (!string.IsNullOrWhiteSpace(apiKey))
            psi.Environment["Server__ApiKey"] = apiKey;

        var proc = Process.Start(psi);
        return new EmbeddedServerHost(proc);
    }

    /// <summary>Returns true when the server process is running.</summary>
    public bool IsRunning => _process is { HasExited: false };

    private static string? FindServerExecutable()
    {
        var baseName = "MtgCollectionTracker.Server";
        string[] candidates =
        [
            Path.Combine(AppContext.BaseDirectory, baseName),
            Path.Combine(AppContext.BaseDirectory, baseName + ".exe"),
        ];
        foreach (var c in candidates)
            if (File.Exists(c))
                return c;
        return null;
    }

    /// <summary>
    /// Sends SIGTERM on Unix so ASP.NET Core can run its graceful-shutdown hooks
    /// (e.g. closing SQLite connections) before the process exits.
    /// On Windows this is a no-op; the caller falls back to <see cref="Process.Kill"/>.
    /// </summary>
    private void RequestGracefulShutdown()
    {
        if (_process is null || _process.HasExited)
            return;

        if (!OperatingSystem.IsWindows())
        {
            try { NativeMethods.SendSigTerm(_process.Id); }
            catch { /* best-effort */ }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_process is { HasExited: false })
        {
            try
            {
                RequestGracefulShutdown();
                // Give the server up to 5 s to shut down cleanly before hard-killing it.
                if (!_process.WaitForExit(5_000))
                    _process.Kill(entireProcessTree: true);
            }
            catch { /* best-effort */ }
        }

        _process?.Dispose();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_process is { HasExited: false })
        {
            try
            {
                RequestGracefulShutdown();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await _process.WaitForExitAsync(cts.Token).ConfigureAwait(false);
            }
            catch
            {
                try { _process?.Kill(entireProcessTree: true); } catch { /* best-effort */ }
            }
        }

        _process?.Dispose();
    }

    private static class NativeMethods
    {
        private const int Sigterm = 15;

        [DllImport("libc", SetLastError = true, EntryPoint = "kill")]
        private static extern int Kill(int pid, int sig);

        internal static void SendSigTerm(int pid) => Kill(pid, Sigterm);
    }
}
