namespace MtgCollectionTracker.Core.Model;

/// <summary>Result returned by the consolidate card-SKUs operation.</summary>
public record ConsolidateResult(int SkusUpdated, int SkusRemoved);
