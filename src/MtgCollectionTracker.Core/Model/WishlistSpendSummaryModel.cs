namespace MtgCollectionTracker.Core.Model;

public record Total(decimal Amount);

public class WishlistSpendSummaryModel
{
    public required Total Total { get; init; }

    public required string[] Vendors { get; init; }

    public required bool IsComplete { get; init; }
}
