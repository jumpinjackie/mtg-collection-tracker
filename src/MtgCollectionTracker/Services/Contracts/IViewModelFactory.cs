using MtgCollectionTracker.ViewModels;

namespace MtgCollectionTracker.Services.Contracts;

public interface IViewModelFactory
{
    CardsViewModel Cards();

    ContainerSetViewModel Containers();

    DeckCollectionViewModel Decks();

    CardSkuItemViewModel CardSku();
    
    ContainerViewModel Container();

    DeckViewModel Deck();

    DrawerViewModel Drawer();

    AddCardsViewModel AddCards();

    DeckListViewModel DeckList();

    ContainerBrowseViewModel BrowseContainer();
}
