using MtgCollectionTracker.ViewModels;
using System.Collections.Generic;

namespace MtgCollectionTracker.Services.Contracts;

public interface IViewModelFactory
{
    CardsViewModel Cards();

    ContainerSetViewModel Containers();

    DeckCollectionViewModel Decks();

    WishlistViewModel Wishlist();

    NotesViewModel Notes();

    CanIBuildThisDeckViewModel CanIBuild();

    WishlistItemViewModel WishListItem();

    SettingsViewModel Settings();

    EditWishlistItemViewModel EditWishlistItem();

    CardSkuItemViewModel CardSku();
    
    ContainerViewModel Container();

    DeckViewModel Deck();

    DialogViewModel Drawer();

    AddCardsViewModel AddCards();

    AddCardsToWishlistViewModel AddCardsToWishlist();

    SendCardsToContainerOrDeckViewModel SendCardsToContainer();

    DeckListViewModel DeckList();

    DeckListVisualViewModel DeckVisual();

    ContainerBrowseViewModel BrowseContainer();

    EditCardSkuViewModel EditCardSku();

    SplitCardSkuViewModel SplitCardSku();

    NewDeckOrContainerViewModel NewDeckOrContainer(DeckOrContainer type);

    ManageVendorsViewModel ManageVendors();
}
