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
    [NotifyPropertyChangedFor(nameof(CanAddSelected))]
    [NotifyPropertyChangedFor(nameof(MaxQuantityToAdd))]
    private SearchResultViewModel? _selectedResult;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAddSelected))]
    private int _quantityToAdd = 1;

    public int MaxQuantityToAdd => SelectedResult?.Quantity ?? 1;

    public bool CanAddSelected => SelectedResult != null && QuantityToAdd >= 1 && QuantityToAdd <= MaxQuantityToAdd;

    public ObservableCollection<SearchResultViewModel> SearchResults { get; } = new();

    partial void OnSelectedResultChanged(SearchResultViewModel? value)
    {
        if (value != null)
        {
            this.QuantityToAdd = 1;
        }
    }

    [RelayCommand]
    private void Search()
    {
        var results = _service.GetCards(new CardQueryModel
        {
            SearchFilter = this.SearchFilter,
            NotInDecks = true
        });

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

    [RelayCommand(CanExecute = nameof(CanAddSelected))]
    private async Task AddSelected(CancellationToken cancel)
    {
        if (SelectedResult == null || !CanAddSelected)
            return;

        var skuId = SelectedResult.SkuId;
        var qty = QuantityToAdd;
        var skuQty = SelectedResult.Quantity;

        Guid transferSkuId;
        if (qty < skuQty)
        {
            // Split the SKU and transfer the new split portion
            var newSku = await _service.SplitCardSkuAsync(new SplitCardSkuInputModel
            {
                CardSkuId = skuId,
                Quantity = qty
            });
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

        await _service.UpdateCardSkuAsync(new UpdateCardSkuInputModel
        {
            Ids = [transferSkuId],
            DeckId = _deckId
        }, null, cancel);

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
