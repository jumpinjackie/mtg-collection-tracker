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

    public DrawerViewModel Drawer() => new DrawerViewModel();

    public AddCardsViewModel AddCards() => new AddCardsViewModel();

    public AddCardsToWishlistViewModel AddCardsToWishlist() => new AddCardsToWishlistViewModel();

    public DeckListViewModel DeckList() => new DeckListViewModel();

    public ContainerBrowseViewModel BrowseContainer() => new ContainerBrowseViewModel();

    public EditCardSkuViewModel EditCardSku() => new EditCardSkuViewModel();

    public SendCardsToContainerOrDeckViewModel SendCardsToContainer(IEnumerable<CardSkuItemViewModel> cards)
        => new SendCardsToContainerOrDeckViewModel { Cards = cards };

    public SplitCardSkuViewModel SplitCardSku() => new SplitCardSkuViewModel();

    public NewDeckOrContainerViewModel NewDeckOrContainer(DeckOrContainer type) => new NewDeckOrContainerViewModel { Type = type };

    public ManageVendorsViewModel ManageVendors() => new ManageVendorsViewModel();
}
