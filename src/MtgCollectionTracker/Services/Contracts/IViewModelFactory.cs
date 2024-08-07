﻿using MtgCollectionTracker.ViewModels;
using System.Collections.Generic;

namespace MtgCollectionTracker.Services.Contracts;

public interface IViewModelFactory
{
    CardsViewModel Cards();

    ContainerSetViewModel Containers();

    DeckCollectionViewModel Decks();

    WishlistViewModel Wishlist();

    WishlistItemViewModel WishListItem();

    EditWishlistItemViewModel EditWishlistItem();

    CardSkuItemViewModel CardSku();
    
    ContainerViewModel Container();

    DeckViewModel Deck();

    DrawerViewModel Drawer();

    AddCardsViewModel AddCards();

    AddCardsToWishlistViewModel AddCardsToWishlist();

    SendCardsToContainerOrDeckViewModel SendCardsToContainer(IEnumerable<CardSkuItemViewModel> cards);

    DeckListViewModel DeckList();

    DeckListVisualViewModel DeckVisual();

    ContainerBrowseViewModel BrowseContainer();

    EditCardSkuViewModel EditCardSku();

    SplitCardSkuViewModel SplitCardSku();

    NewDeckOrContainerViewModel NewDeckOrContainer(DeckOrContainer type);

    ManageVendorsViewModel ManageVendors();
}
