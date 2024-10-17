using CommunityToolkit.Mvvm.ComponentModel;
using MtgCollectionTracker.Core.Model;
using System;

namespace MtgCollectionTracker.ViewModels;

public partial class TempExchangeViewModel : DialogContentViewModel
{
    public int Id { get; set; }

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _fromDecksOrContainers = string.Empty;

    [ObservableProperty]
    private string _toDeck = string.Empty;

    public TempExchangeViewModel WithData(LoanModel loan)
    {
        this.Id = loan.Id;
        this.Name = loan.Name;

        return this;
    }
}
