namespace MtgCollectionTracker.Core.Model;

public class PaginatedCardSkuModel
{
    /// <summary>
    /// Zero-based page number
    /// </summary>
    public int PageNumber { get; set; }

    public int PageSize { get; set; }

    public int Total { get; set; }

    public required IEnumerable<CardSkuModel> Items { get; set; }
}
