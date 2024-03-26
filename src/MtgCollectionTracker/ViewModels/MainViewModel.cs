using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Services.Contracts;
using MtgCollectionTracker.Services.Messaging;
using MtgCollectionTracker.Services.Stubs;

namespace MtgCollectionTracker.ViewModels;

public partial class MainViewModel : RecipientViewModelBase, IRecipient<OpenDrawerMessage>, IRecipient<CloseDrawerMessage>, IRecipient<NotificationMessage>, IRecipient<CardsAddedMessage>
{
    public MainViewModel()
    {
        base.ThrowIfNotDesignMode();
        var vmFactory = new StubViewModelFactory();
        this.Cards = vmFactory.Cards();
        this.Decks = vmFactory.Decks();
        this.Containers = vmFactory.Containers();
        this.IsActive = true;
    }

    public MainViewModel(IViewModelFactory vmFactory)
    {
        this.Cards = vmFactory.Cards();
        this.Decks = vmFactory.Decks();
        this.Containers = vmFactory.Containers();
        this.IsActive = true;
    }

    public CardsViewModel Cards { get; }

    public DeckCollectionViewModel Decks { get; }

    public ContainerSetViewModel Containers { get; }

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
            this.NotificationManager?.Show($"{message.CardsTotal} cards ({message.ProxyTotal} proxies) added to collection");
        else
            this.NotificationManager?.Show($"{message.CardsTotal} cards added to collection");
    }

    public WindowNotificationManager? NotificationManager { get; set; }
}