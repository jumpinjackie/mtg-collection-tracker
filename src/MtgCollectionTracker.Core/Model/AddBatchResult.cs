namespace MtgCollectionTracker.Core.Model;

/// <summary>Result returned by the batch add-cards operation.</summary>
public record AddBatchResult(int Total, int ProxyTotal, int Rows);
