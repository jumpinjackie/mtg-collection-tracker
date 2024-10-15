using MtgCollectionTracker.Services.Contracts;
using MtgCollectionTracker.ViewModels;
using System.Collections.Generic;

namespace MtgCollectionTracker.Services.Stubs;

public class StubViewModelFactory : IViewModelFactory
{
    public CardsViewModel Cards() => new CardsViewModel();

    public CardSkuItemViewModel CardSku() => new CardSkuItemViewModel();

    public WishlistItemViewModel WishListItem() => new WishlistItemViewModel();

    public ContainerViewModel Container() => new ContainerViewModel();

    public DeckViewModel Deck() => new DeckViewModel();

    public DeckCollectionViewModel Decks() => new DeckCollectionViewModel();

    public ContainerSetViewModel Containers() => new ContainerSetViewModel();

    public WishlistViewModel Wishlist() => new WishlistViewModel();

    public NotesViewModel Notes() => new NotesViewModel();

    public CanIBuildThisDeckViewModel CanIBuild() => new CanIBuildThisDeckViewModel();

    public SettingsViewModel Settings() => new SettingsViewModel();

    public DialogViewModel Dialog() => new DialogViewModel();

    public AddCardsViewModel AddCards() => new AddCardsViewModel();

    public AddCardsToWishlistViewModel AddCardsToWishlist() => new AddCardsToWishlistViewModel();

    public DeckDetailsViewModel DeckDetails() => new DeckDetailsViewModel();

    public ContainerBrowseViewModel BrowseContainer() => new ContainerBrowseViewModel();

    public EditCardSkuViewModel EditCardSku() => new EditCardSkuViewModel();

    public SendCardsToContainerOrDeckViewModel SendCardsToContainer() => new SendCardsToContainerOrDeckViewModel();

    public SplitCardSkuViewModel SplitCardSku() => new SplitCardSkuViewModel();

    public NewDeckOrContainerViewModel NewDeckOrContainer(DeckOrContainer type) => new NewDeckOrContainerViewModel { Type = type };

    public EditDeckOrContainerViewModel EditDeckOrContainer(DeckOrContainer type) => new EditDeckOrContainerViewModel { Type = type };

    public ManageVendorsViewModel ManageVendors() => new ManageVendorsViewModel();

    public EditWishlistItemViewModel EditWishlistItem() => new EditWishlistItemViewModel();

    public ContainerTextViewModel ContainerText() => new ContainerTextViewModel();

    public MoveWishlistItemsToCollectionViewModel MoveWishlistItemsToCollection() => new();
}
