using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
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
    int Id { get; }

    int Quantity { get; }

    string CardName { get; }

    string Edition { get; }
}

public partial class SendCardsToContainerOrDeckViewModel : DialogContentViewModel
{
    readonly ICollectionTrackingService _service;
    readonly IScryfallApiClient? _scryfallApiClient;

    readonly Func<ContainerViewModel> _container;
    readonly Func<DeckViewModel> _deck;

    record MockSendableCard(int Id, int Quantity, string CardName, string Edition) : ISendableCardItem;

    public SendCardsToContainerOrDeckViewModel(IMessenger messenger,
                                               Func<ContainerViewModel> container,
                                               Func<DeckViewModel> deck,
                                               ICollectionTrackingService service,
                                               IScryfallApiClient scryfallApiClient)
        : base(messenger)
    {
        _service = service;
        _scryfallApiClient = scryfallApiClient;
        _container = container;
        _deck = deck;
    }

    public SendCardsToContainerOrDeckViewModel()
    {
        this.ThrowIfNotDesignMode();
        _service = new StubCollectionTrackingService();
        _container = () => new();
        _deck = () => new();
        this.AvailableContainers = [
            new ContainerViewModel().WithData(new() { Id = 1, Name = "Main Binder" }),
            new ContainerViewModel().WithData(new() { Id = 2, Name = "Secondary Binder" }),
            new ContainerViewModel().WithData(new() { Id = 3, Name = "Shoe Box" })
        ];
        this.Cards = [
            new MockSendableCard(1, 1, "Black Lotus", "LEB"),
            new MockSendableCard(2, 1, "Mox Jet", "LEB"),
            new MockSendableCard(3, 1, "Mox Ruby", "LEB"),
            new MockSendableCard(4, 1, "Mox Emerald", "LEB"),
            new MockSendableCard(5, 1, "Mox Pearl", "LEB"),
            new MockSendableCard(6, 1, "Mox Sapphire", "LEB"),
            new MockSendableCard(7, 1, "Ancestral Recall", "LEB"),
            new MockSendableCard(8, 1, "Time Walk", "LEB"),
            new MockSendableCard(9, 1, "Timetwister", "LEB")
        ];
        this.AvailableDecks = [
            new DeckViewModel().WithData(new(){ Id = 1, Name = "My Vintage Deck" }),
            new DeckViewModel().WithData(new(){ Id = 1, Name = "My Legacy Deck" })
        ];
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCardsCommand))]
    private ContainerViewModel? _selectedContainer;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCardsCommand))]
    private DeckViewModel? _selectedDeck;

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

    public IEnumerable<ISendableCardItem>? Cards { get; internal set; }

    [MemberNotNullWhen(true, nameof(SelectedContainer), nameof(SelectedDeck), nameof(Cards))]
    private bool CanSendCards() => this.Cards?.Any() == true && (this.SelectedContainer != null || this.SelectedDeck != null || this.UnSetContainer || this.UnSetDeck || this.MarkAsSideboard.HasValue);

    public SendCardsToContainerOrDeckViewModel WithCards(IEnumerable<ISendableCardItem> cards)
    {
        this.Cards = cards;
        this.AvailableContainers = _service.GetContainers().Select(c => _container().WithData(c)).ToList();
        this.AvailableDecks = _service.GetDecks(null).Select(d => _deck().WithData(d)).ToList();
        return this;
    }

    [RelayCommand(CanExecute = nameof(CanSendCards))]
    private async Task SendCards()
    {
        if (CanSendCards())
        {
            var skuIds = this.Cards.Select(c => c.Id).ToList();
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

            Messenger.HandleSkuUpdate(res);
            Messenger.Send(new CloseDialogMessage());
        }
    }
}
