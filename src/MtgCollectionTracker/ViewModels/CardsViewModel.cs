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

public partial class TagSelectionViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private bool _isSelected;
}

public partial class CardsViewModel : RecipientViewModelBase, IRecipient<CardsAddedMessage>, IViewModelWithBusyState, IMultiModeCardListBehaviorHost, IRecipient<TagsAppliedMessage>, IRecipient<CardsSentToContainerMessage>, IRecipient<CardsSentToDeckMessage>, IRecipient<CardSkuSplitMessage>
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
        this.SelectedTags.CollectionChanged += Tags_CollectionChanged;
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
        this.SelectedTags.CollectionChanged += Tags_CollectionChanged;
        this.IsActive = true;
    }

    private void Tags_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        this.PerformSearchCommand.Execute(null);
    }

    IMessenger IViewModelWithBusyState.Messenger => this.Messenger;

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
            this.Tags.Clear();
            foreach (var t in _service.GetTags())
            {
                this.Tags.Add(t);
            }
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

    public ObservableCollection<string> Tags { get; } = new();

    public ObservableCollection<string> SelectedTags { get; } = new();

    [RelayCommand]
    private async Task PerformSearch()
    {
        var hasMinSearchParams = !string.IsNullOrWhiteSpace(this.SearchText) || this.SelectedTags.Count > 0 || this.UnParented;
        if (!hasMinSearchParams)
            return;

        using (((IViewModelWithBusyState)this).StartBusyState())
        {
            this.ShowFirstTimeMessage = false;
            this.ShowSearchResults = true;

            await Task.Delay(500);
            var cards = _service.GetCards(new Core.Model.CardQueryModel
            {
                SearchFilter = this.SearchText,
                Tags = this.SelectedTags.Count > 0 ? this.SelectedTags : null,
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
        Messenger.ToastNotify("Feature not implemented yet");
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
                ViewModel = _vmFactory.Drawer().WithContent("Send Cards To Deck or Container", _vmFactory.SendCardsToContainer().WithCards(Behavior.SelectedItems))
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
                var ids = Behavior.SelectedItems.Select(c => c.Id).ToList();
                var callback = new UpdateCardMetadataProgressCallback
                {
                    OnProgress = (processed, total) =>
                    {
                        Messenger.ToastNotify($"Updated metadata for {processed} of {total} sku(s)");
                    }
                };
                // FIXME: With multiple selections, it seems in general one needs to invoke this twice for the new
                // metadata to stick. I currently don't know why this is the case
                var updatedSkus = await _service.UpdateCardMetadataAsync(ids, _scryfallApiClient, callback, CancellationToken.None);
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
                    $"Are you sure you want to delete this SKU ({sku.Quantity}x {sku.CardName}, {sku.Edition}, {sku.Language ?? "en"})?",
                    async () =>
                    {
                        using (((IViewModelWithBusyState)this).StartBusyState())
                        {
                            await _service.DeleteCardSkuAsync(sku.Id);
                            Messenger.ToastNotify($"Card SKU ({sku.Quantity}x {sku.CardName}, {sku.Edition}, {sku.Language ?? "en"}) deleted");
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

    void IRecipient<CardsSentToContainerMessage>.Receive(CardsSentToContainerMessage message)
    {
        // Un-parented items have no container, so remove from the search results any skus in this
        // list (which have been moved to a new container)
        if (this.UnParented)
        {
            var toRemove = this.SearchResults.Where(r => message.SkuIds.Contains(r.Id)).ToList();
            foreach (var r in toRemove)
                this.SearchResults.Remove(r);
        }
        else // Update existing skus in current search results set to reflect updated containers
        {
            var toUpdate = this.SearchResults
                .Where(r => message.SkuIds.Contains(r.Id))
                .Select(r => r.Id)
                .ToList();
            var updatedSkus = _service.GetCards(new() { CardSkuIds = toUpdate });
            foreach (var sku in updatedSkus)
            {
                this.SearchResults.FirstOrDefault(r => r.Id == sku.Id)?.WithData(sku);
            }
        }
    }

    void IRecipient<CardsSentToDeckMessage>.Receive(CardsSentToDeckMessage message)
    {
        // Un-parented items have no container, so remove from the search results any skus in this
        // list (which have been moved to a deck). Also remove any results that previously were not
        // part of any deck (because they now are)
        if (this.UnParented || this.NotInDecks)
        {
            var toRemove = this.SearchResults.Where(r => message.SkuIds.Contains(r.Id)).ToList();
            foreach (var r in toRemove)
                this.SearchResults.Remove(r);
        }
        else // Update existing skus in current search results set to reflect updated decks
        {
            var toUpdate = this.SearchResults
                .Where(r => message.SkuIds.Contains(r.Id))
                .Select(r => r.Id)
                .ToList();
            var updatedSkus = _service.GetCards(new() { CardSkuIds = toUpdate });
            foreach (var sku in updatedSkus)
            {
                this.SearchResults.FirstOrDefault(r => r.Id == sku.Id)?.WithData(sku);
            }
        }
    }

    void IMultiModeCardListBehaviorHost.HandleBusyChanged(bool oldValue, bool newValue)
    {
        this.OnPropertyChanged(nameof(CanSearch));
    }

    void IRecipient<TagsAppliedMessage>.Receive(TagsAppliedMessage message)
    {
        this.Tags.Clear();
        foreach (var t in message.CurrentTags)
        {
            this.Tags.Add(t);
        }

        var toRemove = this.SelectedTags.Except(message.CurrentTags).ToList();

        // Remove selected tags no longer relevant
        foreach (var st in toRemove)
        {
            this.SelectedTags.Remove(st);
        }
    }

    void IRecipient<CardSkuSplitMessage>.Receive(CardSkuSplitMessage message)
    {
        var toUpdate = this.SearchResults
                .Where(r => r.Id == message.SplitSkuId)
                .Select(r => r.Id)
                .ToList();
        var updatedSkus = _service.GetCards(new() { CardSkuIds = toUpdate });
        foreach (var sku in updatedSkus)
        {
            var item = this.SearchResults.FirstOrDefault(r => r.Id == sku.Id);
            if (item != null)
            {
                item.WithData(sku);
                var idx = this.SearchResults.IndexOf(item);
                // Add the new split sku as well
                var newSku = _service.GetCards(new() { CardSkuIds = [message.NewSkuId] }).ToList();
                if (newSku.Count == 1)
                {
                    this.SearchResults.Insert(idx, _vmFactory.CardSku().WithData(newSku[0]));
                }
            }
        }
    }
}