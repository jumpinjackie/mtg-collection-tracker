using MtgCollectionTracker.ViewModels;
using System.Collections.Generic;

namespace MtgCollectionTracker.Services.Contracts;

public interface IViewModelFactory
{
    CardsViewModel Cards();

    ContainerSetViewModel Containers();

    DeckCollectionViewModel Decks();

    WishlistViewModel Wishlist();

    CardSkuItemViewModel CardSku();
    
    ContainerViewModel Container();

    DeckViewModel Deck();

    DrawerViewModel Drawer();

    AddCardsViewModel AddCards();

    SendCardsToContainerOrDeckViewModel SendCardsToContainer(IEnumerable<CardSkuItemViewModel> cards);

    DeckListViewModel DeckList();

    ContainerBrowseViewModel BrowseContainer();

    EditCardSkuViewModel EditCardSku();

    SplitCardSkuViewModel SplitCardSku();

    NewDeckOrContainerViewModel NewDeckOrContainer(DeckOrContainer type);
}
