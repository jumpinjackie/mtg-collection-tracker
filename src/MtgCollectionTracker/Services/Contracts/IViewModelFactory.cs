using MtgCollectionTracker.ViewModels;

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

    ContainerTextViewModel ContainerText();

    DeckViewModel Deck();

    DialogViewModel Dialog();

    AddCardsViewModel AddCards();

    AddCardsToWishlistViewModel AddCardsToWishlist();

    SendCardsToContainerOrDeckViewModel SendCardsToContainer();

    DeckDetailsViewModel DeckDetails();

    ContainerBrowseViewModel BrowseContainer();

    EditCardSkuViewModel EditCardSku();

    SplitCardSkuViewModel SplitCardSku();

    NewDeckOrContainerViewModel NewDeckOrContainer(DeckOrContainer type);

    EditDeckOrContainerViewModel EditDeckOrContainer(DeckOrContainer type);

    ManageVendorsViewModel ManageVendors();

    MoveWishlistItemsToCollectionViewModel MoveWishlistItemsToCollection();
}
