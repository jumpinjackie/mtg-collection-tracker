namespace MtgCollectionTracker.Core.Model;

public class UpdateLoanModel
{
    public int Id { get; set; }

    public required int[] LoanOutSkus { get; set; }

    public required int[] TakeOutSkus { get; set; }
}
