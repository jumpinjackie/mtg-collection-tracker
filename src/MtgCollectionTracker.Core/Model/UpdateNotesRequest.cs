namespace MtgCollectionTracker.Core.Model;

/// <summary>Request body for creating or updating a notes entry.</summary>
public record UpdateNotesRequest(int? Id, string? Title, string Notes);
