using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services.Contracts;
using MtgCollectionTracker.Services.Stubs;
using System;
using System.Collections.ObjectModel;
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
        this.SelectedCardSkus.CollectionChanged += SelectedCardSkus_CollectionChanged;
    }

    public CardsViewModel(IViewModelFactory vmFactory,
                          ICollectionTrackingService service)
    {
        _vmFactory = vmFactory;
        _service = service;
        this.SelectedCardSkus.CollectionChanged += SelectedCardSkus_CollectionChanged;
    }

    internal void Load()
    {
        var totals = _service.GetCollectionSummary();
        this.CardTotal = totals.CardTotal;
        this.ProxyTotal = totals.ProxyTotal;
        this.SkuTotal = totals.SkuTotal;
        this.DeckTotal = totals.DeckTotal;
        this.ContainerTotal = totals.ContainerTotal;
    }

    public string CollectionSummary => $"{this.CardTotal} cards ({this.ProxyTotal} proxies) across {this.SkuTotal} skus, {this.DeckTotal} decks and {this.ContainerTotal} containers";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CollectionSummary))]
    private int _cardTotal = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CollectionSummary))]
    private int _proxyTotal = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CollectionSummary))]
    private int _skuTotal = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CollectionSummary))]
    private int _deckTotal = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CollectionSummary))]
    private int _containerTotal = 0;

    private void SelectedCardSkus_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        this.HasSelectedCardSku = this.SelectedCardSkus.Count == 1;
        this.CanCombineCardSkus = this.SelectedCardSkus.Count > 1;
        this.CanSplitCardSku = this.SelectedCardSkus.Count == 1;
        this.CanSendSkusToContainer = this.SelectedCardSkus.Count > 0;
        this.CanSendSkusToDeck = this.SelectedCardSkus.Count > 0;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSearch))]
    private string? _searchText;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSearch))]
    private bool _isBusy = false;

    //private CancellationTokenSource? _cancellationTokenSource;

    public ObservableCollection<CardSkuItemViewModel> SelectedCardSkus { get; } = new();

    public bool CanSearch => !string.IsNullOrWhiteSpace(SearchText) && !IsBusy;

    [ObservableProperty]
    private bool _hasSelectedCardSku;

    [ObservableProperty]
    private bool _canCombineCardSkus;

    [ObservableProperty]
    private bool _canSplitCardSku;

    [ObservableProperty]
    private bool _canSendSkusToDeck;

    [ObservableProperty]
    private bool _canSendSkusToContainer;

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
    private void AddSkus()
    {

    }

    [RelayCommand]
    private void ViewSelectedSku()
    {

    }

    [RelayCommand]
    private void SplitSelectedSku()
    {

    }

    [RelayCommand]
    private void CombineSelectedSkus()
    {

    }

    [RelayCommand]
    private void SendSkusToDeck()
    {

    }

    [RelayCommand]
    private void SendSkusToContainer()
    {

    }
}