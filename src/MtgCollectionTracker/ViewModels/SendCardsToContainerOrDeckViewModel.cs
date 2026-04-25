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
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MtgCollectionTracker.ViewModels;

public interface ISendableCardItem
{
    Guid Id { get; }

    int Quantity { get; }

    int? SourceContainerId { get; }

    int? SourceDeckId { get; }

    string CardName { get; }

    string Edition { get; }
}

public partial class SendableCardTransferItemViewModel : ObservableObject
{
    public required Guid Id { get; init; }

    public required int AvailableQuantity { get; init; }

    public int? SourceContainerId { get; init; }

    public int? SourceDeckId { get; init; }

    [ObservableProperty]
    private string _cardName = string.Empty;

    [ObservableProperty]
    private string _edition = string.Empty;

    [ObservableProperty]
    private int _quantityToSend;

    partial void OnQuantityToSendChanged(int value)
    {
        var clamped = Math.Clamp(value, 1, this.AvailableQuantity);
        if (clamped != value)
        {
            this.QuantityToSend = clamped;
        }
    }
}

public partial class SendCardsToContainerOrDeckViewModel : DialogContentViewModel
{
    readonly ICollectionTrackingService _service;
    readonly IScryfallApiClient? _scryfallApiClient;
    readonly SendCardsToContainerOrDeckSelectionState _selectionState;

    readonly Func<ContainerViewModel> _container;
    readonly Func<DeckViewModel> _deck;

    public SendCardsToContainerOrDeckViewModel(IMessenger messenger,
                                               Func<ContainerViewModel> container,
                                               Func<DeckViewModel> deck,
                                               SendCardsToContainerOrDeckSelectionState selectionState,
                                               ICollectionTrackingService service,
                                               IScryfallApiClient scryfallApiClient)
        : base(messenger)
    {
        _service = service;
        _scryfallApiClient = scryfallApiClient;
        _selectionState = selectionState;
        _container = container;
        _deck = deck;
    }

    public SendCardsToContainerOrDeckViewModel()
    {
        this.ThrowIfNotDesignMode();
        _service = new StubCollectionTrackingService();
        _selectionState = new();
        _container = () => new();
        _deck = () => new();
        this.AvailableContainers = [
            new ContainerViewModel().WithData(new() { Id = 1, Name = "Main Binder" }),
            new ContainerViewModel().WithData(new() { Id = 2, Name = "Secondary Binder" }),
            new ContainerViewModel().WithData(new() { Id = 3, Name = "Shoe Box" })
        ];
        this.Cards = [
            new SendableCardTransferItemViewModel { Id = Guid.NewGuid(), AvailableQuantity = 1, QuantityToSend = 1, CardName = "Black Lotus", Edition = "LEB" },
            new SendableCardTransferItemViewModel { Id = Guid.NewGuid(), AvailableQuantity = 1, QuantityToSend = 1, CardName = "Mox Jet", Edition = "LEB" },
            new SendableCardTransferItemViewModel { Id = Guid.NewGuid(), AvailableQuantity = 1, QuantityToSend = 1, CardName = "Mox Ruby", Edition = "LEB" },
            new SendableCardTransferItemViewModel { Id = Guid.NewGuid(), AvailableQuantity = 1, QuantityToSend = 1, CardName = "Mox Emerald", Edition = "LEB" },
            new SendableCardTransferItemViewModel { Id = Guid.NewGuid(), AvailableQuantity = 1, QuantityToSend = 1, CardName = "Mox Pearl", Edition = "LEB" },
            new SendableCardTransferItemViewModel { Id = Guid.NewGuid(), AvailableQuantity = 1, QuantityToSend = 1, CardName = "Mox Sapphire", Edition = "LEB" },
            new SendableCardTransferItemViewModel { Id = Guid.NewGuid(), AvailableQuantity = 1, QuantityToSend = 1, CardName = "Ancestral Recall", Edition = "LEB" },
            new SendableCardTransferItemViewModel { Id = Guid.NewGuid(), AvailableQuantity = 1, QuantityToSend = 1, CardName = "Time Walk", Edition = "LEB" },
            new SendableCardTransferItemViewModel { Id = Guid.NewGuid(), AvailableQuantity = 1, QuantityToSend = 1, CardName = "Timetwister", Edition = "LEB" }
        ];
        this.AvailableDecks = [
            new DeckViewModel().WithData(new() { Id = 1, Format = "Vintage", Name = "[Vintage] My Vintage Deck", DeckName = "My Vintage Deck"}),
            new DeckViewModel().WithData(new() { Id = 2, Format = "Legacy", Name = "[Legacy] My Legacy Deck", DeckName = "My Legacy Deck"}),
        ];
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCardsCommand))]
    [NotifyPropertyChangedFor(nameof(IsUnSetContainerEnabled))]
    [NotifyCanExecuteChangedFor(nameof(ClearSelectedContainerCommand))]
    private ContainerViewModel? _selectedContainer;

    partial void OnSelectedContainerChanged(ContainerViewModel? value)
    {
        if (value != null)
            this.UnSetContainer = false;
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCardsCommand))]
    [NotifyPropertyChangedFor(nameof(IsUnSetDeckEnabled))]
    [NotifyCanExecuteChangedFor(nameof(ClearSelectedDeckCommand))]
    private DeckViewModel? _selectedDeck;

    partial void OnSelectedDeckChanged(DeckViewModel? value)
    {
        if (value != null)
            this.UnSetDeck = false;
    }

    public bool IsUnSetDeckEnabled => this.SelectedDeck == null;

    public bool IsUnSetContainerEnabled => this.SelectedContainer == null;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCardsCommand))]
    private bool _unSetDeck;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCardsCommand))]
    private bool _unSetContainer;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCardsCommand))]
    private bool? _markAsSideboard;

    public IEnumerable<ContainerViewModel>? AvailableContainers { get; internal set; }

    public IEnumerable<DeckViewModel>? AvailableDecks { get; internal set; }

    public IEnumerable<SendableCardTransferItemViewModel>? Cards { get; internal set; }

    [MemberNotNullWhen(true, nameof(SelectedContainer), nameof(SelectedDeck), nameof(Cards))]
    private bool CanSendCards() => this.Cards?.Any() == true && (this.SelectedContainer != null || this.SelectedDeck != null || this.UnSetContainer || this.UnSetDeck || this.MarkAsSideboard.HasValue);

    public async Task<SendCardsToContainerOrDeckViewModel> WithCardsAsync(IEnumerable<ISendableCardItem> cards)
    {
        this.Cards = cards.Select(c => new SendableCardTransferItemViewModel
        {
            Id = c.Id,
            AvailableQuantity = c.Quantity,
            QuantityToSend = c.Quantity,
            SourceContainerId = c.SourceContainerId,
            SourceDeckId = c.SourceDeckId,
            CardName = c.CardName,
            Edition = c.Edition
        }).ToList();
        var containersTask = _service.GetContainersAsync(CancellationToken.None).AsTask();
        var decksTask = _service.GetDecksAsync(null, CancellationToken.None).AsTask();
        await Task.WhenAll(containersTask, decksTask);
        var containers = await containersTask;
        var decks = await decksTask;

        var availableContainers = containers.Select(c => _container().WithData(c)).ToList();
        var availableDecks = decks.Select(d => _deck().WithData(d)).ToList();

        this.AvailableContainers = availableContainers;
        this.AvailableDecks = availableDecks;
        this.SelectedContainer = _selectionState.LastContainerId.HasValue
            ? availableContainers.FirstOrDefault(c => c.Id == _selectionState.LastContainerId.Value)
            : null;
        this.SelectedDeck = _selectionState.LastDeckId.HasValue
            ? availableDecks.FirstOrDefault(d => d.DeckId == _selectionState.LastDeckId.Value)
            : null;
        return this;
    }

    private bool CanClearSelectedContainer() => this.SelectedContainer != null;

    [RelayCommand(CanExecute = nameof(CanClearSelectedContainer))]
    private void ClearSelectedContainer()
    {
        this.SelectedContainer = null;
        this.UnSetContainer = true;
    }

    private bool CanClearSelectedDeck() => this.SelectedDeck != null;

    [RelayCommand(CanExecute = nameof(CanClearSelectedDeck))]
    private void ClearSelectedDeck()
    {
        this.SelectedDeck = null;
        this.UnSetDeck = true;
    }

    [RelayCommand(CanExecute = nameof(CanSendCards))]
    private async Task SendCards()
    {
        if (CanSendCards())
        {
            _selectionState.LastContainerId = this.SelectedContainer?.Id;
            _selectionState.LastDeckId = this.SelectedDeck?.DeckId;

            var cardsToSend = this.Cards.Where(c => c.QuantityToSend >= 1).ToList();
            var skuIds = new List<Guid>(cardsToSend.Count);
            foreach (var card in cardsToSend)
            {
                if (card.QuantityToSend < card.AvailableQuantity)
                {
                    var splitSku = await _service.SplitCardSkuAsync(new SplitCardSkuInputModel
                    {
                        CardSkuId = card.Id,
                        Quantity = card.QuantityToSend
                    }, CancellationToken.None);
                    skuIds.Add(splitSku.Id);
                    Messenger.Send(new CardSkuSplitMessage
                    {
                        SplitSkuId = card.Id,
                        NewSkuId = splitSku.Id,
                        Quantity = card.QuantityToSend,
                        ContainerId = card.SourceContainerId,
                        DeckId = card.SourceDeckId
                    });
                }
                else
                {
                    skuIds.Add(card.Id);
                }
            }

            var res = await _service.UpdateCardSkuAsync(new()
            {
                Ids = skuIds,
                ContainerId = this.SelectedContainer?.Id,
                DeckId = this.SelectedDeck?.DeckId,
                UnsetDeck = this.UnSetDeck,
                UnsetContainer = this.UnSetContainer,
                IsSideboard = this.MarkAsSideboard
            }, _scryfallApiClient, CancellationToken.None);

            if (this.SelectedContainer != null)
            {
                var affectedSkus = res.ChangedContainer().Where(s => s.NewContainerId == this.SelectedContainer.Id).Select(s => s.Id).ToList();
                Messenger.Send(new CardsSentToContainerMessage(this.SelectedContainer.Id, this.SelectedContainer.Name, affectedSkus));
            }
            if (this.SelectedDeck != null)
            {
                var affectedSkus = res.ChangedDecks().Where(s => s.NewDeckId == this.SelectedDeck.DeckId).Select(s => s.Id).ToList();
                Messenger.Send(new CardsSentToDeckMessage(this.SelectedDeck.DeckId, this.SelectedDeck.Name, affectedSkus));
            }
            if (this.UnSetContainer)
            {
                var affectedSkus = res.Skus.Select(s => s.Id).ToList();
                Messenger.Send(new CardsOrphanedMessage(affectedSkus));
            }
            if (this.MarkAsSideboard.HasValue)
            {
                // Notify deck views of cards whose sideboard status changed in the same deck
                foreach (var grp in res.Skus.Where(s => s.NewDeckId.HasValue && s.OldDeckId == s.NewDeckId).GroupBy(s => s.NewDeckId!.Value))
                {
                    Messenger.Send(new DeckSideboardChangedMessage(grp.Key, grp.Select(s => s.Id).ToList(), this.MarkAsSideboard.Value));
                }
            }

            Messenger.HandleSkuUpdate(res);
            Messenger.Send(new CloseDialogMessage());
        }
    }
}
