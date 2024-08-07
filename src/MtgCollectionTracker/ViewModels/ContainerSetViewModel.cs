﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services.Contracts;
using MtgCollectionTracker.Services.Messaging;
using MtgCollectionTracker.Services.Stubs;
using System.Collections.ObjectModel;
using System.Linq;

namespace MtgCollectionTracker.ViewModels;

public partial class ContainerSetViewModel : RecipientViewModelBase, IRecipient<ContainerCreatedMessage>, IRecipient<ContainerDeletedMessage>
{
    readonly IViewModelFactory _vmFactory;
    readonly ICollectionTrackingService _service;

    public ContainerSetViewModel()
    {
        base.ThrowIfNotDesignMode();
        _vmFactory = new StubViewModelFactory();
        _service = new StubCollectionTrackingService();
        this.IsActive = true;
    }

    public ContainerSetViewModel(IViewModelFactory vmFactory, ICollectionTrackingService service)
    {
        _vmFactory = vmFactory;
        _service = service;
        this.IsActive = true;
    }

    protected override void OnActivated()
    {
        if (!Avalonia.Controls.Design.IsDesignMode)
        {
            this.Containers.Clear();
            var containers = _service.GetContainers();
            foreach (var cont in containers)
            {
                this.Containers.Add(_vmFactory.Container().WithData(cont));
            }
        }
        base.OnActivated();
    }

    public ObservableCollection<ContainerViewModel> Containers { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedContainer))]
    private ContainerViewModel? _selectedContainer;

    public bool HasSelectedContainer => this.SelectedContainer != null;

    [RelayCommand]
    private void AddContainer()
    {
        Messenger.Send(new OpenDrawerMessage
        {
            DrawerWidth = 400,
            ViewModel = _vmFactory.Drawer().WithContent("New Container", _vmFactory.NewDeckOrContainer(DeckOrContainer.Container))
        });
    }

    [RelayCommand]
    private void ViewContainer()
    {
        if (this.SelectedContainer != null)
        {
            Messenger.Send(new OpenDrawerMessage
            {
                DrawerWidth = 1000,
                ViewModel = _vmFactory.Drawer().WithContent(this.SelectedContainer.Name, _vmFactory.BrowseContainer().WithContainerId(this.SelectedContainer.Id))
            });
        }
    }

    void IRecipient<ContainerCreatedMessage>.Receive(ContainerCreatedMessage message)
    {
        this.Containers.Add(_vmFactory.Container().WithData(message.Container));
    }

    void IRecipient<ContainerDeletedMessage>.Receive(ContainerDeletedMessage message)
    {
        var item = this.Containers.FirstOrDefault(c => c.Id == message.Id);
        if (item != null)
        {
            this.Containers.Remove(item);
        }
    }
}
