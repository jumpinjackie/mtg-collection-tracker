using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services;
using MtgCollectionTracker.Services.Messaging;
using MtgCollectionTracker.Services.Stubs;
using ScryfallApi.Client;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MtgCollectionTracker.ViewModels;

public partial class AddExistingCardsToDeckViewModel : DialogContentViewModel
{
    readonly ICollectionTrackingService _service;
    readonly IScryfallApiClient? _scryfallApiClient;

    private int _deckId;
    private string _deckName = string.Empty;

    public AddExistingCardsToDeckViewModel()
    {
        this.ThrowIfNotDesignMode();
        _service = new StubCollectionTrackingService();
        this.SearchResults.Add(new SearchResultViewModel { SkuId = Guid.NewGuid(), CardName = "Black Lotus", Edition = "LEB", Quantity = 1, Location = "Main Binder" });
        this.SearchResults.Add(new SearchResultViewModel { SkuId = Guid.NewGuid(), CardName = "Mox Pearl", Edition = "LEB", Quantity = 2, Location = "Main Binder" });
        this.HasSearchResults = true;
        this.PendingItems.Add(new PendingItemViewModel { SkuId = Guid.NewGuid(), CardName = "Ancestral Recall", Edition = "LEB", AvailableQty = 1, QtyToAdd = 1, Location = "Main Binder" });
        this.HasPendingItems = true;
    }

    public AddExistingCardsToDeckViewModel(IMessenger messenger,
                                           ICollectionTrackingService service,
                                           IScryfallApiClient scryfallApiClient)
        : base(messenger)
    {
        _service = service;
        _scryfallApiClient = scryfallApiClient;
    }

    public AddExistingCardsToDeckViewModel WithDeck(int deckId, string deckName)
    {
        _deckId = deckId;
        _deckName = deckName;
        this.OnPropertyChanged(nameof(DeckName));
        return this;
    }

    public string DeckName => _deckName;

    [ObservableProperty]
    private string _searchFilter = string.Empty;

    [ObservableProperty]
    private bool _hasSearchResults;

    [ObservableProperty]
    private bool _hasNoResults;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddToDeckCommand))]
    private bool _hasPendingItems;

    [ObservableProperty]
    private bool _addToSideboard;

    public ObservableCollection<SearchResultViewModel> SearchResults { get; } = new();

    public ObservableCollection<PendingItemViewModel> PendingItems { get; } = new();

    private bool CanAddToDeck() => HasPendingItems;

    [RelayCommand]
    private void Search()
    {
        var results = _service.GetCardsAsync(new CardQueryModel
        {
            SearchFilter = this.SearchFilter,
            NotInDecks = true
        }, System.Threading.CancellationToken.None).GetAwaiter().GetResult();

        this.SearchResults.Clear();
        foreach (var sku in results)
        {
            this.SearchResults.Add(new SearchResultViewModel
            {
                SkuId = sku.Id,
                CardName = sku.CardName,
                Edition = sku.Edition,
                Quantity = sku.Quantity,
                Location = sku.ContainerName ?? "(unparented)"
            });
        }

        this.HasSearchResults = this.SearchResults.Count > 0;
        this.HasNoResults = this.SearchResults.Count == 0;
    }

    [RelayCommand]
    private void AddToStaging(SearchResultViewModel item)
    {
        // Skip if already staged
        if (this.PendingItems.Any(p => p.SkuId == item.SkuId))
            return;

        this.PendingItems.Add(new PendingItemViewModel
        {
            SkuId = item.SkuId,
            CardName = item.CardName,
            Edition = item.Edition,
            AvailableQty = item.Quantity,
            QtyToAdd = 1,
            Location = item.Location
        });

        this.HasPendingItems = true;
    }

    [RelayCommand]
    private void RemovePendingItem(PendingItemViewModel item)
    {
        this.PendingItems.Remove(item);
        this.HasPendingItems = this.PendingItems.Count > 0;
    }

    [RelayCommand(CanExecute = nameof(CanAddToDeck))]
    private async Task AddToDeck(CancellationToken cancel)
    {
        var itemsToAdd = this.PendingItems.Where(r => r.QtyToAdd >= 1).ToList();
        if (itemsToAdd.Count == 0)
            return;

        foreach (var item in itemsToAdd)
        {
            var skuId = item.SkuId;
            var qty = item.QtyToAdd;
            var skuQty = item.AvailableQty;

            Guid transferSkuId;
            if (qty < skuQty)
            {
                // Split the SKU and transfer the new split portion
                var newSku = await _service.SplitCardSkuAsync(new SplitCardSkuInputModel
                {
                    CardSkuId = skuId,
                    Quantity = qty
                }, cancel);
                transferSkuId = newSku.Id;

                Messenger.Send(new CardSkuSplitMessage
                {
                    SplitSkuId = skuId,
                    NewSkuId = newSku.Id,
                    Quantity = qty
                });
            }
            else
            {
                // Transfer the whole SKU
                transferSkuId = skuId;
            }

            var res = await _service.UpdateCardSkuAsync(new UpdateCardSkuInputModel
            {
                Ids = [transferSkuId],
                DeckId = _deckId,
                IsSideboard = this.AddToSideboard
            }, null, cancel);

            Messenger.HandleSkuUpdate(res);
        }

        Messenger.Send(new DeckTotalsChangedMessage([_deckId]));
        Messenger.Send(new CardsAddedToDeckMessage(_deckId));
        Messenger.Send(new CloseDialogMessage());
    }

    [RelayCommand]
    private void Cancel()
    {
        Messenger.Send(new CloseDialogMessage());
    }
}

public partial class SearchResultViewModel : ObservableObject
{
    public Guid SkuId { get; set; }

    [ObservableProperty]
    private string _cardName = string.Empty;

    [ObservableProperty]
    private string _edition = string.Empty;

    [ObservableProperty]
    private int _quantity;

    [ObservableProperty]
    private string _location = string.Empty;
}

public partial class PendingItemViewModel : ObservableObject
{
    public Guid SkuId { get; set; }

    [ObservableProperty]
    private string _cardName = string.Empty;

    [ObservableProperty]
    private string _edition = string.Empty;

    [ObservableProperty]
    private int _availableQty;

    [ObservableProperty]
    private int _qtyToAdd;

    [ObservableProperty]
    private string _location = string.Empty;
}

