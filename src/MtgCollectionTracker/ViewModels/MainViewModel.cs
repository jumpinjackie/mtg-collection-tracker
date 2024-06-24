using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Services;
using MtgCollectionTracker.Services.Contracts;
using MtgCollectionTracker.Services.Messaging;
using MtgCollectionTracker.Services.Stubs;
using System.Linq;

namespace MtgCollectionTracker.ViewModels;

public partial class MainViewModel : RecipientViewModelBase, IRecipient<OpenDrawerMessage>, IRecipient<CloseDrawerMessage>, IRecipient<NotificationMessage>, IRecipient<CardsAddedMessage>, IRecipient<CardsSentToContainerMessage>, IRecipient<CardsSentToDeckMessage>, IRecipient<CardsAddedToWishlistMessage>, IRecipient<WishlistItemUpdatedMessage>
{
    public MainViewModel()
    {
        base.ThrowIfNotDesignMode();
        var vmFactory = new StubViewModelFactory();
        this.Cards = vmFactory.Cards();
        this.Decks = vmFactory.Decks();
        this.Containers = vmFactory.Containers();
        this.Wishlist = vmFactory.Wishlist();
        this.IsActive = true;
    }

    public MainViewModel(IViewModelFactory vmFactory)
    {
        this.Cards = vmFactory.Cards();
        this.Decks = vmFactory.Decks();
        this.Containers = vmFactory.Containers();
        this.Wishlist = vmFactory.Wishlist();
        this.IsActive = true;
    }

    public CardsViewModel Cards { get; }

    public DeckCollectionViewModel Decks { get; }

    public ContainerSetViewModel Containers { get; }

    public WishlistViewModel Wishlist { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDrawerOpen))]
    private DrawerViewModel? _drawer;

    public bool IsDrawerOpen => this.Drawer != null;

    [ObservableProperty]
    private int _drawerWidth;

    void IRecipient<OpenDrawerMessage>.Receive(OpenDrawerMessage message)
    {
        this.DrawerWidth = message.DrawerWidth;
        this.Drawer = message.ViewModel;
        //this.NotificationManager?.Show("Drawer Opened");
    }

    void IRecipient<CloseDrawerMessage>.Receive(CloseDrawerMessage message)
    {
        this.Drawer = null;
        //this.NotificationManager?.Show("Drawer Closed");
    }

    void IRecipient<NotificationMessage>.Receive(NotificationMessage message)
    {
        this.NotificationManager?.Show(message.Content);
    }

    void IRecipient<CardsAddedMessage>.Receive(CardsAddedMessage message)
    {
        if (message.ProxyTotal > 0)
            this.NotificationManager?.Show($"{message.CardsTotal} card(s) ({message.ProxyTotal} proxies) added to collection");
        else
            this.NotificationManager?.Show($"{message.CardsTotal} card(s) added to collection");
    }

    void IRecipient<CardsAddedToWishlistMessage>.Receive(CardsAddedToWishlistMessage message)
    {
        this.NotificationManager?.Show($"{message.Added.Sum(s => s.Quantity)} card(s) added to wishlist");
    }

    void IRecipient<CardsSentToContainerMessage>.Receive(CardsSentToContainerMessage message)
    {
        this.NotificationManager?.Show($"{message.TotalSkus} SKU(s) moved to container ({message.ContainerName})");
    }

    void IRecipient<CardsSentToDeckMessage>.Receive(CardsSentToDeckMessage message)
    {
        this.NotificationManager?.Show($"{message.TotalSkus} SKU(s) moved to deck ({message.DeckName})");
    }

    void IRecipient<WishlistItemUpdatedMessage>.Receive(WishlistItemUpdatedMessage message)
    {
        this.NotificationManager?.Show("Wishlist item updated");
    }

    public WindowNotificationManager? NotificationManager { get; set; }
}