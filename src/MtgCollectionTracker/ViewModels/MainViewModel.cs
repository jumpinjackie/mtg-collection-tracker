using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Services.Contracts;
using MtgCollectionTracker.Services.Messaging;
using MtgCollectionTracker.Services.Stubs;
using System.Collections.Generic;
using System.Linq;

namespace MtgCollectionTracker.ViewModels;

public partial class MainViewModel : RecipientViewModelBase, IRecipient<OpenDialogMessage>, IRecipient<CloseDialogMessage>, IRecipient<NotificationMessage>, IRecipient<CardsAddedMessage>, IRecipient<CardsSentToContainerMessage>, IRecipient<CardsSentToDeckMessage>, IRecipient<CardsAddedToWishlistMessage>, IRecipient<WishlistItemUpdatedMessage>
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

    private Stack<DialogViewModel> _dialogStack = new();

    void IRecipient<OpenDialogMessage>.Receive(OpenDialogMessage message)
    {
        // HACK: Cannot figure out if/how DialogHost can have multiple dialogs open at once but
        // visually stacked. As a workaround if there is a pre-existing dialog, dislodge that one
        // and show our new one, but keep references to open dialogs in a stack
        if (_dialogStack.Count > 0)
            DialogHostAvalonia.DialogHost.Close(null);

        _dialogStack.Push(message.ViewModel);
        DialogHostAvalonia.DialogHost.Show(_dialogStack.Peek());
    }

    void IRecipient<CloseDialogMessage>.Receive(CloseDialogMessage message)
    {
        // HACK: Cannot figure out if/how DialogHost can have multiple dialogs open at once but
        // visually stacked. As a workaround if there are multiple "open" dialogs, then close the
        // topmost one, pop its VM off the stack and restore the next topmost dialog VM

        // Right now we are assuming the close message was sent from the topmost dialog VM, which
        // right now is a safe assumption to make
        if (_dialogStack.Count > 0)
        {
            _dialogStack.Pop();
            DialogHostAvalonia.DialogHost.Close(null);

            if (_dialogStack.Count > 0)
                DialogHostAvalonia.DialogHost.Show(_dialogStack.Peek());
        }
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