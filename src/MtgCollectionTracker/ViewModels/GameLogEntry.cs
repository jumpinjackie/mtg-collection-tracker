using System;

namespace MtgCollectionTracker.ViewModels;

/// <summary>
/// Represents a single timestamped entry in the game action log.
/// </summary>
public class GameLogEntry
{
    public DateTime Timestamp { get; } = DateTime.Now;

    public string Message { get; }

    public GameLogEntry(string message)
    {
        Message = message;
    }

    public string DisplayText => $"[{Timestamp:HH:mm:ss}] {Message}";
}
