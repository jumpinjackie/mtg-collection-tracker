using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services.Contracts;
using MtgCollectionTracker.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MtgCollectionTracker.Services;

public class ViewModelFactory : IViewModelFactory
{
    readonly Func<CardsViewModel> _cards;
    readonly Func<CardSkuItemViewModel> _cardSku;
    readonly Func<WishlistItemViewModel> _wishlistItem;
    readonly Func<DeckViewModel> _deck;
    readonly Func<DeckCollectionViewModel> _decks;
    readonly Func<ContainerSetViewModel> _containerSet;
    readonly Func<WishlistViewModel> _wishlist;
    readonly Func<CanIBuildThisDeckViewModel> _canIBuild;
    readonly Func<SettingsViewModel> _settings;
    readonly Func<NotesViewModel> _notes;
    readonly Func<ContainerViewModel> _container;
    readonly Func<DialogViewModel> _drawer;
    readonly Func<AddCardsViewModel> _addCards;
    readonly Func<AddCardsToWishlistViewModel> _addCardsToWishlist;
    readonly Func<DeckDetailsViewModel> _deckDetails;
    readonly Func<ContainerBrowseViewModel> _browseContainer;
    readonly Func<EditCardSkuViewModel> _editCardSku;
    readonly Func<SplitCardSkuViewModel> _splitCardSku;
    readonly Func<NewDeckOrContainerViewModel> _newDeck;
    readonly Func<EditDeckOrContainerViewModel> _editDeck;
    readonly Func<SendCardsToContainerOrDeckViewModel> _sendCardsToContainer;
    readonly Func<ManageVendorsViewModel> _manageVendors;
    readonly Func<EditWishlistItemViewModel> _editWishlistItem;

    public ViewModelFactory(Func<CardsViewModel> cards,
                            Func<CardSkuItemViewModel> cardSku,
                            Func<WishlistItemViewModel> wishlistItem,
                            Func<DeckViewModel> deck,
                            Func<DeckCollectionViewModel> decks,
                            Func<ContainerSetViewModel> containerSet,
                            Func<WishlistViewModel> wishlist,
                            Func<CanIBuildThisDeckViewModel> canIBuild,
                            Func<SettingsViewModel> settings,
                            Func<NotesViewModel> notes,
                            Func<ContainerViewModel> container,
                            Func<DialogViewModel> drawer,
                            Func<AddCardsViewModel> addCards,
                            Func<AddCardsToWishlistViewModel> addCardsToWishlist,
                            Func<DeckDetailsViewModel> deckDetails,
                            Func<ContainerBrowseViewModel> browseContainer,
                            Func<EditCardSkuViewModel> editCardSku,
                            Func<SplitCardSkuViewModel> splitCardSku,
                            Func<NewDeckOrContainerViewModel> newDeck,
                            Func<EditDeckOrContainerViewModel> editDeck,
                            Func<SendCardsToContainerOrDeckViewModel> sendCardsToContainer,
                            Func<ManageVendorsViewModel> manageVendors,
                            Func<EditWishlistItemViewModel> editWishlistItem)
    {
        _cards = cards;
        _cardSku = cardSku;
        _wishlistItem = wishlistItem;
        _deck = deck;
        _decks = decks;
        _containerSet = containerSet;
        _wishlist = wishlist;
        _canIBuild = canIBuild;
        _settings = settings;
        _notes = notes;
        _container = container;
        _drawer = drawer;
        _addCards = addCards;
        _addCardsToWishlist = addCardsToWishlist;
        _deckDetails = deckDetails;
        _browseContainer = browseContainer;
        _editCardSku = editCardSku;
        _splitCardSku = splitCardSku;
        _newDeck = newDeck;
        _editDeck = editDeck;
        _sendCardsToContainer = sendCardsToContainer;
        _manageVendors = manageVendors;
        _editWishlistItem = editWishlistItem;
    }

    public CardsViewModel Cards() => _cards();

    public CardSkuItemViewModel CardSku() => _cardSku();

    public WishlistItemViewModel WishListItem() => _wishlistItem();

    public DeckViewModel Deck() => _deck();

    public DeckCollectionViewModel Decks() => _decks();

    public ContainerSetViewModel Containers() => _containerSet();

    public WishlistViewModel Wishlist() => _wishlist();

    public NotesViewModel Notes() => _notes();

    public CanIBuildThisDeckViewModel CanIBuild() => _canIBuild();

    public SettingsViewModel Settings() => _settings();

    public ContainerViewModel Container() => _container();

    public DialogViewModel Drawer() => _drawer();

    public AddCardsViewModel AddCards() => _addCards();

    public AddCardsToWishlistViewModel AddCardsToWishlist() => _addCardsToWishlist();

    public DeckDetailsViewModel DeckDetails() => _deckDetails();

    public ContainerBrowseViewModel BrowseContainer() => _browseContainer();

    public EditCardSkuViewModel EditCardSku() => _editCardSku();

    public SendCardsToContainerOrDeckViewModel SendCardsToContainer() => _sendCardsToContainer();

    public SplitCardSkuViewModel SplitCardSku() => _splitCardSku();

    public NewDeckOrContainerViewModel NewDeckOrContainer(DeckOrContainer type)
    {
        var vm = _newDeck();
        vm.Type = type;
        return vm;
    }

    public EditDeckOrContainerViewModel EditDeckOrContainer(DeckOrContainer type)
    {
        var vm = _editDeck();
        vm.Type = type;
        return vm;
    }

    public ManageVendorsViewModel ManageVendors() => _manageVendors();

    public EditWishlistItemViewModel EditWishlistItem() => _editWishlistItem();
}
