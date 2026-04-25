namespace MtgCollectionTracker.Core.Model;

/// <summary>Request body for creating or updating a deck.</summary>
public record CreateDeckRequest(string Name, string? Format, int? ContainerId, bool IsCommander);
