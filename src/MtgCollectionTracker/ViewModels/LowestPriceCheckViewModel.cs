using System.Collections.Generic;
using System.Collections.ObjectModel;
using MtgCollectionTracker.Core.Services;

namespace MtgCollectionTracker.ViewModels;

public partial class LowestPriceCheckViewModel : DialogContentViewModel
{
    public ObservableCollection<LowestPriceCheckItem> Cards { get; } = new();

    public bool IsComplete { get; set; }

    public decimal Total { get; set; }

    public LowestPriceCheckViewModel WithCards(IEnumerable<LowestPriceCheckItem> cards)
    {
        this.Total = 0;
        bool complete = true;
        foreach (var c in cards)
        {
            this.Cards.Add(c);
            if (!c.ItemTotal.HasValue)
                complete = false;
            else
                this.Total += c.QuantityTotal!.Value;
        }
        this.IsComplete = complete;
        return this;
    }
}