namespace MtgCollectionTracker.Core.Model;

public class LoanModel
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required ICollection<CardSkuModel> CardsOnLoan { get; set; }

    public int ToDeckId { get; set; }
}
