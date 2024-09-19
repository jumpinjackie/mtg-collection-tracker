namespace MtgCollectionTracker.Core.Model;

public class FetchContainerPageModel
{
    /// <summary>
    /// If true, only return skus with missing metadata
    /// </summary>
    public bool ShowOnlyMissingMetadata { get; set; }

    /// <summary>
    /// Zero-based page number
    /// </summary>
    public required int PageNumber { get; set; }

    /// <summary>
    /// Page size. If not specified, defaults to 12
    /// </summary>
    public int? PageSize { get; set; }
}
