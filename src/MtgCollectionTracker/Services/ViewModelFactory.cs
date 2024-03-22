using MtgCollectionTracker.Services.Contracts;
using MtgCollectionTracker.ViewModels;
using System;

namespace MtgCollectionTracker.Services;

public class ViewModelFactory : IViewModelFactory
{
    readonly Func<CardsViewModel> _cards;
    readonly Func<CardSkuItemViewModel> _cardSku;
    readonly Func<DeckViewModel> _deck;
    readonly Func<DeckCollectionViewModel> _decks;
    readonly Func<ContainerSetViewModel> _containerSet;
    readonly Func<ContainerViewModel> _container;

    public ViewModelFactory(Func<CardsViewModel> cards,
                            Func<CardSkuItemViewModel> cardSku,
                            Func<DeckViewModel> deck,
                            Func<DeckCollectionViewModel> decks,
                            Func<ContainerSetViewModel> containerSet,
                            Func<ContainerViewModel> container)
    {
        _cards = cards;
        _cardSku = cardSku;
        _deck = deck;
        _decks = decks;
        _containerSet = containerSet;
        _container = container;
    }

    public CardsViewModel Cards() => _cards();

    public CardSkuItemViewModel CardSku() => _cardSku();

    public DeckViewModel Deck() => _deck();

    public DeckCollectionViewModel Decks() => _decks();

    public ContainerSetViewModel Containers() => _containerSet();

    public ContainerViewModel Container() => _container();
}
