﻿using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using DialogHostAvalonia.Positioners;
using MtgCollectionTracker.Services;
using MtgCollectionTracker.Services.Contracts;
using MtgCollectionTracker.Services.Messaging;
using MtgCollectionTracker.Services.Stubs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MtgCollectionTracker.ViewModels;

public partial class MainViewModel : RecipientViewModelBase, IRecipient<OpenDialogMessage>, IRecipient<CloseDialogMessage>, IRecipient<NotificationMessage>, IRecipient<CardsAddedMessage>, IRecipient<CardsSentToContainerMessage>, IRecipient<CardsSentToDeckMessage>, IRecipient<CardsAddedToWishlistMessage>, IRecipient<WishlistItemUpdatedMessage>, IRecipient<GlobalBusyMessage>
{
    public MainViewModel()
    {
        base.ThrowIfNotDesignMode();
        this.Cards = new();
        this.Decks = new();
        this.Containers = new();
        this.Wishlist = new();
        this.Notes = new();
        this.CanIBuild = new();
        this.Settings = new();
        this.IsActive = true;
    }

    public MainViewModel(Func<CardsViewModel> cards,
                         Func<DeckCollectionViewModel> decks,
                         Func<ContainerSetViewModel> containers,
                         Func<WishlistViewModel> wishlist,
                         Func<NotesViewModel> notes,
                         Func<CanIBuildThisDeckViewModel> canIBuild,
                         Func<SettingsViewModel> settings)
    {
        this.Cards = cards();
        this.Decks = decks();
        this.Containers = containers();
        this.Wishlist = wishlist();
        this.Notes = notes();
        this.CanIBuild = canIBuild();
        this.Settings = settings();
        this.IsActive = true;
    }

    public IDialogPopupPositioner DialogPositioner { get; } = new DialogPopupPositioner();

    public CardsViewModel Cards { get; }

    public DeckCollectionViewModel Decks { get; }

    public ContainerSetViewModel Containers { get; }

    public WishlistViewModel Wishlist { get; }

    public CanIBuildThisDeckViewModel CanIBuild { get; }

    public NotesViewModel Notes { get; }

    public SettingsViewModel Settings { get; }

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
        this.NotificationManager?.Show(message.Content, message.Type);
    }

    void IRecipient<CardsAddedMessage>.Receive(CardsAddedMessage message)
    {
        if (message.ProxyTotal > 0)
            this.NotificationManager?.Show($"{message.CardsTotal} card(s) ({message.ProxyTotal} proxies) added to collection", NotificationType.Success);
        else
            this.NotificationManager?.Show($"{message.CardsTotal} card(s) added to collection", NotificationType.Success);
    }

    void IRecipient<CardsAddedToWishlistMessage>.Receive(CardsAddedToWishlistMessage message)
    {
        this.NotificationManager?.Show($"{message.Added.Sum(s => s.Quantity)} card(s) added to wishlist", NotificationType.Success);
    }

    void IRecipient<CardsSentToContainerMessage>.Receive(CardsSentToContainerMessage message)
    {
        this.NotificationManager?.Show($"{message.SkuIds.Count} SKU(s) moved to container ({message.ContainerName})", NotificationType.Success);
    }

    void IRecipient<CardsSentToDeckMessage>.Receive(CardsSentToDeckMessage message)
    {
        this.NotificationManager?.Show($"{message.SkuIds.Count} SKU(s) moved to deck ({message.DeckName})", NotificationType.Success);
    }

    void IRecipient<WishlistItemUpdatedMessage>.Receive(WishlistItemUpdatedMessage message)
    {
        this.NotificationManager?.Show("Wishlist item updated", NotificationType.Success);
    }

    void IRecipient<GlobalBusyMessage>.Receive(GlobalBusyMessage message)
    {
        this.IsBusy = message.IsBusy;
    }

    [ObservableProperty]
    private bool _isBusy;

    public WindowNotificationManager? NotificationManager { get; set; }
}