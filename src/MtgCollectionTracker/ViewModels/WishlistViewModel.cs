using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services.Stubs;
using System.Collections.ObjectModel;

namespace MtgCollectionTracker.ViewModels;

public partial class WishlistViewModel : RecipientViewModelBase
{
    readonly ICollectionTrackingService _service;

    public WishlistViewModel()
    {
        this.ThrowIfNotDesignMode();
        _service = new StubCollectionTrackingService();
        this.IsActive = true;
    }

    public WishlistViewModel(ICollectionTrackingService service)
    {
        _service = service;
        this.IsActive = true;
    }

    public ObservableCollection<object> Cards { get; } = new();

    public ObservableCollection<object> SelectedItems { get; } = new();

    [ObservableProperty]
    private bool _isBusy;

    public bool IsEmptyCollection => Cards.Count == 0;

    [ObservableProperty]
    private bool _hasSelectedItems;

    [RelayCommand]
    private void AddCards()
    {

    }

    [RelayCommand]
    private void DeleteCards()
    {

    }

    [RelayCommand]
    private void ManageVendors()
    {

    }

    [RelayCommand]
    private void MoveToInventory()
    {

    }

    [RelayCommand]
    private void EditPriceData()
    {

    }
}
