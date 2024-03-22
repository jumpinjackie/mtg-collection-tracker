using MtgCollectionTracker.ViewModels;

namespace MtgCollectionTracker.Services.Contracts;

public interface IViewModelFactory
{
    CardsViewModel Cards();

    CardSkuItemViewModel CardSku();
}
