using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services.Contracts;
using MtgCollectionTracker.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MtgCollectionTracker.Services;

public class ViewModelFactory : IViewModelFactory
{
    readonly Func<CardsViewModel> _cards;
    readonly Func<CardSkuItemViewModel> _cardSku;
    readonly Func<DeckViewModel> _deck;
    readonly Func<DeckCollectionViewModel> _decks;
    readonly Func<ContainerSetViewModel> _containerSet;
    readonly Func<ContainerViewModel> _container;
    readonly Func<DrawerViewModel> _drawer;
    readonly Func<AddCardsViewModel> _addCards;
    readonly Func<DeckListViewModel> _deckList;
    readonly Func<ContainerBrowseViewModel> _browseContainer;
    readonly Func<EditCardSkuViewModel> _editCardSku;
    readonly Func<SplitCardSkuViewModel> _splitCardSku;
    readonly Func<NewDeckOrContainerViewModel> _newDeck;
    readonly Func<SendCardsToContainerOrDeckViewModel> _sendCardsToContainer;

    readonly ICollectionTrackingService _service;

    public ViewModelFactory(ICollectionTrackingService service,
                            Func<CardsViewModel> cards,
                            Func<CardSkuItemViewModel> cardSku,
                            Func<DeckViewModel> deck,
                            Func<DeckCollectionViewModel> decks,
                            Func<ContainerSetViewModel> containerSet,
                            Func<ContainerViewModel> container,
                            Func<DrawerViewModel> drawer,
                            Func<AddCardsViewModel> addCards,
                            Func<DeckListViewModel> deckList,
                            Func<ContainerBrowseViewModel> browseContainer,
                            Func<EditCardSkuViewModel> editCardSku,
                            Func<SplitCardSkuViewModel> splitCardSku,
                            Func<NewDeckOrContainerViewModel> newDeck,
                            Func<SendCardsToContainerOrDeckViewModel> sendCardsToContainer)
    {
        _service = service;

        _cards = cards;
        _cardSku = cardSku;
        _deck = deck;
        _decks = decks;
        _containerSet = containerSet;
        _container = container;
        _drawer = drawer;
        _addCards = addCards;
        _deckList = deckList;
        _browseContainer = browseContainer;
        _editCardSku = editCardSku;
        _splitCardSku = splitCardSku;
        _newDeck = newDeck;
        _sendCardsToContainer = sendCardsToContainer;
    }

    public CardsViewModel Cards() => _cards();

    public CardSkuItemViewModel CardSku() => _cardSku();

    public DeckViewModel Deck() => _deck();

    public DeckCollectionViewModel Decks() => _decks();

    public ContainerSetViewModel Containers() => _containerSet();

    public ContainerViewModel Container() => _container();

    public DrawerViewModel Drawer() => _drawer();

    public AddCardsViewModel AddCards() => _addCards();

    public DeckListViewModel DeckList() => _deckList();

    public ContainerBrowseViewModel BrowseContainer() => _browseContainer();

    public EditCardSkuViewModel EditCardSku() => _editCardSku();

    public SendCardsToContainerOrDeckViewModel SendCardsToContainer(IEnumerable<CardSkuItemViewModel> cards)
    {
        var vm = _sendCardsToContainer();
        vm.Cards = cards;
        vm.AvailableContainers = _service.GetContainers().Select(c => _container().WithData(c)).ToList();
        vm.AvailableDecks = _service.GetDecks(null).Select(d => _deck().WithData(d)).ToList();
        return vm;
    }

    public SplitCardSkuViewModel SplitCardSku() => _splitCardSku();

    public NewDeckOrContainerViewModel NewDeckOrContainer(DeckOrContainer type)
    {
        var vm = _newDeck();
        vm.Type = type;
        return vm;
    }
}
