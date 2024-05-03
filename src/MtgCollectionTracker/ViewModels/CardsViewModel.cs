using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services;
using MtgCollectionTracker.Services.Contracts;
using MtgCollectionTracker.Services.Messaging;
using MtgCollectionTracker.Services.Stubs;
using ScryfallApi.Client;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MtgCollectionTracker.ViewModels;

public partial class CardsViewModel : RecipientViewModelBase, IRecipient<CardsAddedMessage>, IViewModelWithBusyState
{
    readonly IViewModelFactory _vmFactory;
    readonly ICollectionTrackingService _service;
    readonly IScryfallApiClient _scryfallApiClient;

    public CardsViewModel()
    {
        base.ThrowIfNotDesignMode();
        _vmFactory = new StubViewModelFactory();
        _service = new StubCollectionTrackingService();
        this.SelectedCardSkus.CollectionChanged += SelectedCardSkus_CollectionChanged;
        this.IsActive = true;
    }

    public CardsViewModel(IMessenger messenger,
                          IViewModelFactory vmFactory,
                          ICollectionTrackingService service,
                          IScryfallApiClient scryfallApiClient)
        : base(messenger)
    {
        _vmFactory = vmFactory;
        _service = service;
        _scryfallApiClient = scryfallApiClient;
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
        this.HasMultipleSelectedCardSkus = this.SelectedCardSkus.Count > 1;
        this.HasSelectedCardSku = this.SelectedCardSkus.Count == 1;
        this.HasAtLeastOneSelectedCardSku = this.SelectedCardSkus.Count > 0;
    }

    public bool CanCombineCardSkus => !this.IsBusy && this.HasMultipleSelectedCardSkus;
    public bool CanSplitCardSku => !this.IsBusy && this.HasSelectedCardSku;
    public bool CanSendSkusToContainer => !this.IsBusy && this.HasAtLeastOneSelectedCardSku;
    public bool CanSendSkusToDeck => !this.IsBusy && this.HasAtLeastOneSelectedCardSku;
    public bool CanUpdateMetadata => !this.IsBusy && this.HasAtLeastOneSelectedCardSku;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSearch))]
    private string? _searchText;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSearch))]
    [NotifyPropertyChangedFor(nameof(CanSendSkusToContainer))]
    [NotifyPropertyChangedFor(nameof(CanSendSkusToDeck))]
    [NotifyPropertyChangedFor(nameof(CanUpdateMetadata))]
    [NotifyPropertyChangedFor(nameof(CanCombineCardSkus))]
    [NotifyPropertyChangedFor(nameof(CanSplitCardSku))]
    private bool _isBusy = false;

    //private CancellationTokenSource? _cancellationTokenSource;

    public ObservableCollection<CardSkuItemViewModel> SelectedCardSkus { get; } = new();

    public bool CanSearch => !string.IsNullOrWhiteSpace(SearchText) && !IsBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSendSkusToContainer))]
    [NotifyPropertyChangedFor(nameof(CanSendSkusToDeck))]
    [NotifyPropertyChangedFor(nameof(CanUpdateMetadata))]
    private bool _hasAtLeastOneSelectedCardSku;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCombineCardSkus))]
    private bool _hasMultipleSelectedCardSkus;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSplitCardSku))]
    private bool _hasSelectedCardSku;

    public ObservableCollection<CardSkuItemViewModel> SearchResults { get; } = new();

    [ObservableProperty]
    private bool _noProxies;

    [ObservableProperty]
    private bool _notInDecks;

    [ObservableProperty]
    private bool _hasNoResults;

    [ObservableProperty]
    private bool _unParented;

    [RelayCommand]
    private async Task PerformSearch()
    {
        if (string.IsNullOrEmpty(SearchText))
            return;

        using (((IViewModelWithBusyState)this).StartBusyState())
        {
            this.ShowFirstTimeMessage = false;
            this.ShowSearchResults = true;

            await Task.Delay(1000);
            var cards = _service.GetCards(new Core.Model.CardQueryModel
            {
                SearchFilter = this.SearchText,
                NoProxies = this.NoProxies,
                NotInDecks = this.NotInDecks,
                UnParented = this.UnParented,
                IncludeScryfallMetadata = true
            });
            this.SearchResults.Clear();
            foreach (var sku in cards)
            {
                this.SearchResults.Add(_vmFactory.CardSku().WithData(sku));
            }
            this.HasNoResults = !this.ShowFirstTimeMessage && this.SearchResults.Count == 0;
        }
    }

    [RelayCommand]
    private void AddSkus()
    {
        Messenger.Send(new OpenDrawerMessage
        {
            DrawerWidth = 800,
            ViewModel = _vmFactory.Drawer().WithContent("Add Cards", _vmFactory.AddCards())
        });
    }

    partial void OnNoProxiesChanged(bool value)
    {
        this.PerformSearchCommand.Execute(null);
    }

    partial void OnNotInDecksChanged(bool value)
    {
        this.PerformSearchCommand.Execute(null);
    }

    partial void OnUnParentedChanged(bool value)
    {
        this.PerformSearchCommand.Execute(null);
    }

    [RelayCommand]
    private void ViewSelectedSku()
    {

    }

    [RelayCommand]
    private void EditSelectedSku()
    {
        if (this.SelectedCardSkus.Count == 1)
        {
            Messenger.Send(new OpenDrawerMessage
            {
                DrawerWidth = 600,
                ViewModel = _vmFactory.Drawer().WithContent("Edit Sku", _vmFactory.EditCardSku().WithSku(this.SelectedCardSkus[0]))
            });
        }
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
        if (this.SelectedCardSkus.Count > 0)
        {

        }
    }

    [RelayCommand]
    private void SendSkusToContainer()
    {
        if (this.SelectedCardSkus.Count > 0)
        {
            Messenger.Send(new OpenDrawerMessage
            {
                DrawerWidth = 800,
                ViewModel = _vmFactory.Drawer().WithContent("Send Cards To Container", _vmFactory.SendCardsToContainer(this.SelectedCardSkus))
            });
        }
    }

    [RelayCommand]
    private async Task UpdateSkuMetadata()
    {
        if (this.SelectedCardSkus.Count > 0)
        {
            using (((IViewModelWithBusyState)this).StartBusyState())
            {
                int updated = 0;
                var ids = this.SelectedCardSkus.Select(c => c.Id);
                var updatedSkus = await _service.UpdateCardMetadataAsync(ids, _scryfallApiClient, CancellationToken.None);
                foreach (var sku in updatedSkus)
                {
                    var skuM = this.SearchResults.FirstOrDefault(c => c.Id == sku.Id);
                    if (skuM != null)
                    {
                        skuM.WithData(sku);
                        updated++;
                    }
                }
                if (updated > 0)
                {
                    Messenger.ToastNotify($"Metadata updated for {updated} sku(s)");
                }
            }
        }
    }

    [RelayCommand]
    private void DeleteSku()
    {
        if (this.SelectedCardSkus.Count == 1)
        {
            var sku = this.SelectedCardSkus[0];
            Messenger.Send(new OpenDrawerMessage
            {
                DrawerWidth = 400,
                ViewModel = _vmFactory.Drawer().WithConfirmation(
                    "Delete Card SKU",
                    $"Are you sure you want to delete this SKU?",
                    async () =>
                    {
                        using (((IViewModelWithBusyState)this).StartBusyState())
                        {
                            await _service.DeleteCardSkuAsync(sku.Id);
                            Messenger.ToastNotify($"Card SKU ({sku.CardName}, {sku.Language ?? "en"}) deleted");
                            this.SelectedCardSkus.Remove(sku);
                            this.SearchResults.Remove(sku);
                            this.SkuTotal -= 1;
                            this.ProxyTotal -= sku.ProxyQty;
                            this.CardTotal -= sku.RealQty;
                            Messenger.Send(new CloseDrawerMessage());
                        }
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