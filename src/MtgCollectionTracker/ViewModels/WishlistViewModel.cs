using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services.Contracts;
using MtgCollectionTracker.Services.Messaging;
using MtgCollectionTracker.Services.Stubs;
using ScryfallApi.Client.Apis;
using System.Collections.ObjectModel;

namespace MtgCollectionTracker.ViewModels;

public partial class WishlistViewModel : RecipientViewModelBase, IRecipient<CardsAddedToWishlistMessage>
{
    readonly IViewModelFactory _vmFactory;
    readonly ICollectionTrackingService _service;

    public WishlistViewModel()
    {
        this.ThrowIfNotDesignMode();
        _vmFactory = new StubViewModelFactory();
        _service = new StubCollectionTrackingService();
        this.IsActive = true;
    }

    public WishlistViewModel(IViewModelFactory vmFactory, ICollectionTrackingService service)
    {
        _vmFactory = vmFactory;
        _service = service;
        this.IsActive = true;
    }

    protected override void OnActivated()
    {
        if (!Avalonia.Controls.Design.IsDesignMode)
        {
            var items = _service.GetWishlistItems();
            foreach (var item in items)
            {
                this.Cards.Add(_vmFactory.WishListItem().WithData(item));
            }
        }
        base.OnActivated();
    }

    public ObservableCollection<object> Cards { get; } = new();

    public ObservableCollection<object> SelectedItems { get; } = new();

    [ObservableProperty]
    private bool _isBusy;

    public bool IsEmptyCollection => Cards.Count == 0;

    [ObservableProperty]
    private bool _hasSelectedItems;

    [RelayCommand]
    private void AddCards()
    {
        Messenger.Send(new OpenDrawerMessage
        {
            DrawerWidth = 800,
            ViewModel = _vmFactory.Drawer().WithContent("Add Cards to Wishlist", _vmFactory.AddCardsToWishlist())
        });
    }

    [RelayCommand]
    private void DeleteCards()
    {

    }

    [RelayCommand]
    private void ManageVendors()
    {

    }

    [RelayCommand]
    private void MoveToInventory()
    {

    }

    [RelayCommand]
    private void EditPriceData()
    {

    }

    void IRecipient<CardsAddedToWishlistMessage>.Receive(CardsAddedToWishlistMessage message)
    {
        foreach (var item in message.Added)
        {
            this.Cards.Add(_vmFactory.WishListItem().WithData(item));
        }
    }
}
