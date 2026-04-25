namespace MtgCollectionTracker.Core.Model;

/// <summary>Request body for creating or updating a storage container.</summary>
public record CreateContainerRequest(string Name, string? Description);
