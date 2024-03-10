using MtgCollectionTracker.Data;

namespace MtgCollectionTracker.Core.Model;

public class ContainerSummaryModel : ContainerInfoModel
{
    public int Total { get; set; }

    public virtual ICollection<CardSku> Cards { get; set; }
}
