using MtgCollectionTracker.Services.Contracts;
using MtgCollectionTracker.ViewModels;

namespace MtgCollectionTracker.Services.Stubs;

public class StubViewModelFactory : IViewModelFactory
{
    public CardsViewModel Cards() => new CardsViewModel();

    public CardSkuItemViewModel CardSku() => new CardSkuItemViewModel();
}
