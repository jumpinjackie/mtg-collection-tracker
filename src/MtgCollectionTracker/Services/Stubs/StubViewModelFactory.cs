using MtgCollectionTracker.Services.Contracts;
using MtgCollectionTracker.ViewModels;

namespace MtgCollectionTracker.Services.Stubs;

public class StubViewModelFactory : IViewModelFactory
{
    public CardsViewModel Cards() => new CardsViewModel();

    public CardSkuItemViewModel CardSku() => new CardSkuItemViewModel();

    public ContainerViewModel Container() => new ContainerViewModel();

    public DeckViewModel Deck() => new DeckViewModel();

    public DeckCollectionViewModel Decks() => new DeckCollectionViewModel();

    public ContainerSetViewModel Containers() => new ContainerSetViewModel();

    public DrawerViewModel Drawer() => new DrawerViewModel();

    public AddCardsViewModel AddCards() => new AddCardsViewModel();

    public DeckListViewModel DeckList() => new DeckListViewModel();

    public ContainerBrowseViewModel BrowseContainer() => new ContainerBrowseViewModel();

    public EditCardSkuViewModel EditCardSku() => new EditCardSkuViewModel();
}
