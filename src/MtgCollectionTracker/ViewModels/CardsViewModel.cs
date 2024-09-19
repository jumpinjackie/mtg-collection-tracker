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

public partial class CardsViewModel : RecipientViewModelBase, IRecipient<CardsAddedMessage>, IViewModelWithBusyState, IMultiModeCardListBehaviorHost
{
    readonly IViewModelFactory _vmFactory;
    readonly ICollectionTrackingService _service;
    readonly IScryfallApiClient _scryfallApiClient;

    public CardsViewModel()
    {
        base.ThrowIfNotDesignMode();
        _vmFactory = new StubViewModelFactory();
        _service = new StubCollectionTrackingService();
        this.Behavior = new(this);
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
        this.Behavior = new(this);
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

    public MultiModeCardListBehavior<CardSkuItemViewModel> Behavior { get; }

    public ObservableCollection<CardSkuItemViewModel> SearchResults { get; } = new();

    bool IViewModelWithBusyState.IsBusy
    {
        get => Behavior.IsBusy;
        set => Behavior.IsBusy = value;
    }

    [ObservableProperty]
    private bool _noProxies;

    [ObservableProperty]
    private bool _notInDecks;

    [ObservableProperty]
    private bool _hasNoResults;

    [RelayCommand]
    private async Task PerformSearch()
    {
        if (!this.UnParented && string.IsNullOrEmpty(this.SearchText))
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
        Messenger.Send(new OpenDialogMessage
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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSearch))]
    private string? _searchText;

    [ObservableProperty]
    private bool _unParented;

    public bool CanSearch
    {
        get
        {
            if (((IViewModelWithBusyState)this).IsBusy)
                return false;

            if (!this.UnParented)
                return !string.IsNullOrWhiteSpace(SearchText);

            return true;
        }
    }

    [RelayCommand]
    private void ViewSelectedSku()
    {

    }

    [RelayCommand]
    private void EditSelectedSku()
    {
        if (Behavior.SelectedItems.Count == 1)
        {
            Messenger.Send(new OpenDialogMessage
            {
                DrawerWidth = 600,
                ViewModel = _vmFactory.Drawer().WithContent("Edit Sku", _vmFactory.EditCardSku().WithSku(Behavior.SelectedItems[0]))
            });
        }
        else if (Behavior.SelectedItems.Count > 1)
        {
            Messenger.Send(new OpenDialogMessage
            {
                DrawerWidth = 600,
                ViewModel = _vmFactory.Drawer().WithContent("Edit Skus", _vmFactory.EditCardSku().WithSkus(Behavior.SelectedItems))
            });
        }
    }

    [RelayCommand]
    private void SplitSelectedSku()
    {
        if (Behavior.IsItemSplittable)
        {
            var selected = Behavior.SelectedItems[0];
            var vm = _vmFactory.SplitCardSku();
            vm.CardSkuId = selected.Id;
            if (selected.ProxyQty > 1)
            {
                vm.CurrentQuantity = selected.ProxyQty;
            }
            else if (selected.RealQty > 1)
            {
                vm.CurrentQuantity = selected.RealQty;
            }
            if (vm.CurrentQuantity == 0)
                return;

            vm.SplitQuantity = vm.SplitMin;
            Messenger.Send(new OpenDialogMessage
            {
                DrawerWidth = 300,
                ViewModel = _vmFactory.Drawer().WithContent("Split Card SKU", vm)
            });
        }
    }

    [RelayCommand]
    private void SendSkusToContainer()
    {
        if (Behavior.SelectedItems.Count > 0)
        {
            Messenger.Send(new OpenDialogMessage
            {
                DrawerWidth = 800,
                ViewModel = _vmFactory.Drawer().WithContent("Send Cards To Deck or Container", _vmFactory.SendCardsToContainer(Behavior.SelectedItems))
            });
        }
    }

    [RelayCommand]
    private async Task UpdateSkuMetadata()
    {
        if (Behavior.SelectedItems.Count > 0)
        {
            using (((IViewModelWithBusyState)this).StartBusyState())
            {
                int updated = 0;
                var ids = Behavior.SelectedItems.Select(c => c.Id);
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
        if (Behavior.SelectedItems.Count == 1)
        {
            var sku = Behavior.SelectedItems[0];
            Messenger.Send(new OpenDialogMessage
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
                            Behavior.SelectedItems.Remove(sku);
                            this.SearchResults.Remove(sku);
                            this.SkuTotal -= 1;
                            this.ProxyTotal -= sku.ProxyQty;
                            this.CardTotal -= sku.RealQty;
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

    void IMultiModeCardListBehaviorHost.HandleBusyChanged(bool oldValue, bool newValue)
    {
        this.OnPropertyChanged(nameof(CanSearch));
    }
}