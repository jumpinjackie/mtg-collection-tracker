﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services;
using MtgCollectionTracker.Services.Contracts;
using MtgCollectionTracker.Services.Messaging;
using MtgCollectionTracker.Services.Stubs;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace MtgCollectionTracker.ViewModels;

public partial class CardsViewModel : RecipientViewModelBase, IRecipient<CardsAddedMessage>
{
    readonly IMessenger _messenger;
    readonly IViewModelFactory _vmFactory;
    readonly ICollectionTrackingService _service;

    public CardsViewModel()
    {
        base.ThrowIfNotDesignMode();
        _messenger = WeakReferenceMessenger.Default;
        _vmFactory = new StubViewModelFactory();
        _service = new StubCollectionTrackingService();
        this.SelectedCardSkus.CollectionChanged += SelectedCardSkus_CollectionChanged;
        this.IsActive = true;
    }

    public CardsViewModel(IMessenger messenger,
                          IViewModelFactory vmFactory,
                          ICollectionTrackingService service)
    {
        _messenger = messenger;
        _vmFactory = vmFactory;
        _service = service;
        this.SelectedCardSkus.CollectionChanged += SelectedCardSkus_CollectionChanged;
        this.IsActive = true;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        if (Avalonia.Controls.Design.IsDesignMode)
        {
            ApplyTotals(CollectionSummaryModel.Empty());
        }
        else
        {
            var totals = _service.GetCollectionSummary();
            ApplyTotals(totals);
        }
    }

    private void ApplyTotals(CollectionSummaryModel totals)
    {
        this.CardTotal = totals.CardTotal;
        this.ProxyTotal = totals.ProxyTotal;
        this.SkuTotal = totals.SkuTotal;

        this.ShowFirstTimeMessage = !this.IsEmptyCollection;
    }

    public string CollectionSummary => $"{this.CardTotal} cards ({this.ProxyTotal} proxies) across {this.SkuTotal} skus";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CollectionSummary))]
    [NotifyPropertyChangedFor(nameof(IsEmptyCollection))]
    private int _cardTotal = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CollectionSummary))]
    [NotifyPropertyChangedFor(nameof(IsEmptyCollection))]
    private int _proxyTotal = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CollectionSummary))]
    [NotifyPropertyChangedFor(nameof(IsEmptyCollection))]
    private int _skuTotal = 0;

    public bool IsEmptyCollection => this.CardTotal == 0 && this.ProxyTotal == 0;

    [ObservableProperty]
    private bool _showSearchResults = false;

    [ObservableProperty]
    private bool _showFirstTimeMessage = false;

    [ObservableProperty]
    private bool _showEmptyMessage = false;

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

    [ObservableProperty]
    private bool _noProxies;

    [ObservableProperty]
    private bool _notInDecks;

    [ObservableProperty]
    private bool _hasNoResults;

    [RelayCommand]
    private async Task PerformSearch()
    {
        this.IsBusy = true;
        this.ShowFirstTimeMessage = false;
        this.ShowSearchResults = true;
        try
        {
            await Task.Delay(1000);
            var cards = _service.GetCards(new Core.Model.CardQueryModel
            {
                SearchFilter = this.SearchText,
                NoProxies = this.NoProxies,
                NotInDecks = this.NotInDecks
            });
            this.SearchResults.Clear();
            foreach (var sku in cards)
            {
                this.SearchResults.Add(_vmFactory.CardSku().WithData(sku));
            }
            this.HasNoResults = !this.ShowFirstTimeMessage && this.SearchResults.Count == 0;
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
        _messenger.Send(new OpenDrawerMessage
        {
            DrawerWidth = 800,
            ViewModel = _vmFactory.Drawer().WithContent("Add Cards", _vmFactory.AddCards())
        });
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

    [RelayCommand]
    private void DeleteSku()
    {
        if (this.SelectedCardSkus.Count == 1)
        {
            var sku = this.SelectedCardSkus[0];
            _messenger.Send(new OpenDrawerMessage
            {
                DrawerWidth = 400,
                ViewModel = _vmFactory.Drawer().WithConfirmation(
                    "Delete Card SKU",
                    $"Are you sure you want to delete this SKU?",
                    async () =>
                    {
                        await _service.DeleteCardSkuAsync(sku.Id);
                        _messenger.ToastNotify($"Card SKU ({sku.CardName}) deleted");
                        this.SelectedCardSkus.Remove(sku);
                        this.SearchResults.Remove(sku);
                        this.SkuTotal -= 1;
                        this.ProxyTotal -= sku.ProxyQty;
                        this.CardTotal -= sku.RealQty;
                        _messenger.Send(new CloseDrawerMessage());
                    })
            });
        }
    }

    void IRecipient<CardsAddedMessage>.Receive(CardsAddedMessage message)
    {
        this.SkuTotal += message.SkuTotal;
        this.ProxyTotal += message.ProxyTotal;
        this.CardTotal += message.CardsTotal;
    }
}