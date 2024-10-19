using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services;
using MtgCollectionTracker.Services.Messaging;
using MtgCollectionTracker.Services.Stubs;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace MtgCollectionTracker.ViewModels;

public partial class ContainerSetViewModel : RecipientViewModelBase, IRecipient<ContainerCreatedMessage>, IRecipient<ContainerDeletedMessage>, IRecipient<ContainerUpdatedMessage>
{
    readonly ICollectionTrackingService _service;
    readonly Func<DialogViewModel> _dialog;
    readonly Func<NewDeckOrContainerViewModel> _newDeckOrContainer;
    readonly Func<EditDeckOrContainerViewModel> _editDeckOrContainer;
    readonly Func<ContainerBrowseViewModel> _browseContainer;
    readonly Func<ContainerTextViewModel> _containerText;
    readonly Func<ContainerViewModel> _container;

    public ContainerSetViewModel()
    {
        base.ThrowIfNotDesignMode();
        _service = new StubCollectionTrackingService();
        _dialog = () => new();
        _editDeckOrContainer = () => new();
        _newDeckOrContainer = () => new();
        _browseContainer = () => new();
        _containerText = () => new();
        _container = () => new();
        this.IsActive = true;
    }

    public ContainerSetViewModel(Func<DialogViewModel> dialog,
                                 Func<NewDeckOrContainerViewModel> newDeckOrContainer,
                                 Func<EditDeckOrContainerViewModel> editDeckOrContainer,
                                 Func<ContainerBrowseViewModel> browseContainer,
                                 Func<ContainerTextViewModel> containerText,
                                 Func<ContainerViewModel> container,
                                 ICollectionTrackingService service)
    {
        _service = service;
        _dialog = dialog;
        _editDeckOrContainer = editDeckOrContainer;
        _newDeckOrContainer = newDeckOrContainer;
        _browseContainer = browseContainer;
        _containerText = containerText;
        _container = container;
        this.IsActive = true;
    }

    protected override void OnActivated()
    {
        if (!Avalonia.Controls.Design.IsDesignMode)
        {
            this.RefreshListCommand.Execute(null);
        }
        base.OnActivated();
    }

    public ObservableCollection<ContainerViewModel> Containers { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanRunAgainstSelectedContainer))]
    private ContainerViewModel? _selectedContainer;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanRunAgainstSelectedContainer))]
    private bool _isBusy;

    public bool CanRunAgainstSelectedContainer => SelectedContainer != null && !IsBusy;

    [RelayCommand]
    private void AddContainer()
    {
        Messenger.Send(new OpenDialogMessage
        {
            DrawerWidth = 400,
            ViewModel = _dialog().WithContent("New Container", _newDeckOrContainer().WithType(DeckOrContainer.Container))
        });
    }

    [RelayCommand]
    private void EditContainer()
    {
        if (this.SelectedContainer != null)
        {
            Messenger.Send(new OpenDialogMessage
            {
                DrawerWidth = 400,
                ViewModel = _dialog().WithContent("Edit Container", _editDeckOrContainer().WithType(DeckOrContainer.Container).WithContainer(this.SelectedContainer.Id, this.SelectedContainer.Name, this.SelectedContainer.Description))
            });
        }
    }

    [RelayCommand]
    private void DeleteContainer()
    {
        if (this.SelectedContainer != null)
        {
            Messenger.Send(new OpenDialogMessage
            {
                DrawerWidth = 800,
                ViewModel = _dialog().WithConfirmation(
                    "Delete Container?",
                    $"Are you sure you want to delete ({this.SelectedContainer.Name})? All SKUs in this container will be un-assigned",
                    async () =>
                    {
                        try
                        {
                            var res = await _service.DeleteContainerAsync(new() { ContainerId = this.SelectedContainer.Id });
                            this.Messenger.ToastNotify($"Container Deleted. {res.UnassignedSkuTotal} SKU(s) un-assigned", Avalonia.Controls.Notifications.NotificationType.Success);
                            this.Messenger.Send(new ContainerDeletedMessage { Id = this.SelectedContainer.Id });
                        }
                        catch (Exception ex)
                        {
                            this.Messenger.ToastNotify($"Error deleting container: {ex.Message}", Avalonia.Controls.Notifications.NotificationType.Error);
                        }
                    })
            });
        }
    }

    [RelayCommand]
    private void ViewContainer()
    {
        if (this.SelectedContainer != null)
        {
            Messenger.Send(new OpenDialogMessage
            {
                DrawerWidth = 1000,
                ViewModel = _dialog().WithContent(this.SelectedContainer.Name, _browseContainer().WithContainerId(this.SelectedContainer.Id))
            });
        }
    }

    [RelayCommand]
    private void ViewContainerText()
    {
        if (this.SelectedContainer != null)
        {
            var text = _service.PrintContainer(this.SelectedContainer.Id, true);
            Messenger.Send(new OpenDialogMessage
            {
                DrawerWidth = 1000,
                ViewModel = _dialog().WithContent(this.SelectedContainer.Name, _containerText().WithText(text))
            });
        }
    }

    [RelayCommand]
    private void RefreshList()
    {
        this.Containers.Clear();
        var containers = _service.GetContainers();
        foreach (var cont in containers)
        {
            this.Containers.Add(_container().WithData(cont));
        }
    }

    void IRecipient<ContainerCreatedMessage>.Receive(ContainerCreatedMessage message)
    {
        this.Containers.Add(_container().WithData(message.Container));
    }

    void IRecipient<ContainerDeletedMessage>.Receive(ContainerDeletedMessage message)
    {
        var item = this.Containers.FirstOrDefault(c => c.Id == message.Id);
        if (item != null)
        {
            this.Containers.Remove(item);
        }
    }

    void IRecipient<ContainerUpdatedMessage>.Receive(ContainerUpdatedMessage message)
    {
        var item = this.Containers.FirstOrDefault(c => c.Id == message.Container.Id);
        if (item != null)
        {
            item.WithData(message.Container);
        }
    }
}
