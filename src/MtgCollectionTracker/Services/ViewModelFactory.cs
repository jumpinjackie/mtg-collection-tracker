using MtgCollectionTracker.Services.Contracts;
using MtgCollectionTracker.ViewModels;
using System;

namespace MtgCollectionTracker.Services;

public class ViewModelFactory : IViewModelFactory
{
    readonly Func<CardsViewModel> _cards;
    readonly Func<CardSkuItemViewModel> _cardSku;

    public ViewModelFactory(Func<CardsViewModel> cards,
                            Func<CardSkuItemViewModel> cardSku)
    {
        _cards = cards;
        _cardSku = cardSku;
    }

    public CardsViewModel Cards() => _cards();

    public CardSkuItemViewModel CardSku() => _cardSku();
}
