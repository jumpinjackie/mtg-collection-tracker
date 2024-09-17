using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services.Messaging;
using MtgCollectionTracker.Services.Stubs;
using ScryfallApi.Client;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MtgCollectionTracker.ViewModels;

public partial class SendCardsToContainerOrDeckViewModel : DialogContentViewModel
{
    readonly ICollectionTrackingService _service;
    readonly IScryfallApiClient? _scryfallApiClient;

    public SendCardsToContainerOrDeckViewModel(IMessenger messenger, ICollectionTrackingService service, IScryfallApiClient scryfallApiClient)
        : base(messenger)
    {
        _service = service;
        _scryfallApiClient = scryfallApiClient;
    }

    public SendCardsToContainerOrDeckViewModel()
    {
        this.ThrowIfNotDesignMode();
        _service = new StubCollectionTrackingService();
        this.AvailableContainers = [
            new ContainerViewModel().WithData(new() { Id = 1, Name = "Main Binder" }),
            new ContainerViewModel().WithData(new() { Id = 2, Name = "Secondary Binder" }),
            new ContainerViewModel().WithData(new() { Id = 3, Name = "Shoe Box" })
        ];
        this.Cards = [
            new CardSkuItemViewModel().WithData(new() { Quantity = 1, CardName = "Black Lotus", Edition = "LEB" }),
                new CardSkuItemViewModel().WithData(new() { Quantity = 1, CardName = "Mox Jet", Edition = "LEB" }),
                new CardSkuItemViewModel().WithData(new() { Quantity = 1, CardName = "Mox Ruby", Edition = "LEB" }),
                new CardSkuItemViewModel().WithData(new() { Quantity = 1, CardName = "Mox Emerald", Edition = "LEB" }),
                new CardSkuItemViewModel().WithData(new() { Quantity = 1, CardName = "Mox Pearl", Edition = "LEB" }),
                new CardSkuItemViewModel().WithData(new() { Quantity = 1, CardName = "Mox Sapphire", Edition = "LEB" }),
                new CardSkuItemViewModel().WithData(new() { Quantity = 1, CardName = "Ancestral Recall", Edition = "LEB" }),
                new CardSkuItemViewModel().WithData(new() { Quantity = 1, CardName = "Time Walk", Edition = "LEB" }),
                new CardSkuItemViewModel().WithData(new() { Quantity = 1, CardName = "Timetwister", Edition = "LEB" })
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
    private bool _unSetDeck;

    [ObservableProperty]
    private bool _unSetContainer;

    public IEnumerable<ContainerViewModel>? AvailableContainers { get; internal set; }

    public IEnumerable<DeckViewModel>? AvailableDecks { get; internal set; }

    public IEnumerable<CardSkuItemViewModel>? Cards { get; internal set; }

    private bool CanSendCards() => this.SelectedContainer != null || this.SelectedDeck != null;

    [RelayCommand(CanExecute = nameof(CanSendCards))]
    private async Task SendCards()
    {
        if (this.SelectedContainer != null || this.SelectedDeck != null)
        {
            var skuIds = this.Cards.Select(c => c.Id);
            var res = await _service.UpdateCardSkuAsync(new()
            {
                Ids = skuIds,
                ContainerId = this.SelectedContainer?.Id,
                DeckId = this.SelectedDeck?.DeckId,
                UnsetDeck = this.UnSetDeck,
                UnsetContainer = this.UnSetContainer
            }, _scryfallApiClient, CancellationToken.None);
            /*
            // Update existing selection with updated model
            var updatedSkus = _service.GetCards(new() { CardSkuIds = skuIds });
            foreach (var sku in updatedSkus)
            {
                var c = this.Cards.FirstOrDefault(card => card.Id == sku.Id);
                if (c != null)
                    c.WithData(sku);
            }
            */
            if (this.SelectedContainer != null)
                Messenger.Send(new CardsSentToContainerMessage(res, this.SelectedContainer.Name));
            if (this.SelectedDeck != null)
                Messenger.Send(new CardsSentToDeckMessage(res, this.SelectedDeck.Name));
            Messenger.Send(new CloseDialogMessage());
        }
    }
}
