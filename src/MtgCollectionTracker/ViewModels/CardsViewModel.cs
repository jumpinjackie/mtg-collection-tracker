using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services.Contracts;
using MtgCollectionTracker.Services.Stubs;
using System;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace MtgCollectionTracker.ViewModels;

public partial class CardsViewModel : ViewModelBase
{
    readonly IViewModelFactory _vmFactory;
    readonly ICollectionTrackingService _service;

    public CardsViewModel()
    {
        base.ThrowIfNotDesignMode();
        _vmFactory = new StubViewModelFactory();
        _service = new StubCollectionTrackingService();
    }

    public CardsViewModel(IViewModelFactory vmFactory,
                          ICollectionTrackingService service)
    {
        _vmFactory = vmFactory;
        _service = service;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSearch))]
    private string? _searchText;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSearch))]
    private bool _isBusy = false;

    //private CancellationTokenSource? _cancellationTokenSource;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedCardSku))]
    private CardSkuItemViewModel? _selectedCardSku;

    public bool CanSearch => !string.IsNullOrWhiteSpace(SearchText) && !IsBusy;

    public bool HasSelectedCardSku => SelectedCardSku != null;

    public ObservableCollection<CardSkuItemViewModel> SearchResults { get; } = new();

    [RelayCommand]
    private async Task PerformSearch()
    {
        this.IsBusy = true;
        try
        {
            await Task.Delay(1000);
            var cards = _service.GetCards(new Core.Model.CardQueryModel
            {
                SearchFilter = this.SearchText
            });
            this.SearchResults.Clear();
            foreach (var sku in cards)
            {
                this.SearchResults.Add(_vmFactory.CardSku().WithData(sku));
            }
        }
        catch (Exception ex)
        {

        }
        finally
        {
            this.IsBusy = false;
        }
    }

    [RelayCommand]
    private void ViewCardSku()
    {

    }
}