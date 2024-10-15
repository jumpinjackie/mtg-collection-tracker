using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services;
using MtgCollectionTracker.Services.Contracts;
using MtgCollectionTracker.Services.Messaging;
using MtgCollectionTracker.Services.Stubs;
using System.Collections.ObjectModel;
using System.Linq;

namespace MtgCollectionTracker.ViewModels;

public partial class ContainerSetViewModel : RecipientViewModelBase, IRecipient<ContainerCreatedMessage>, IRecipient<ContainerDeletedMessage>, IRecipient<ContainerUpdatedMessage>
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
            ViewModel = _vmFactory.Dialog().WithContent("New Container", _vmFactory.NewDeckOrContainer(DeckOrContainer.Container))
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
                ViewModel = _vmFactory.Dialog().WithContent("Edit Container", _vmFactory.EditDeckOrContainer(DeckOrContainer.Container).WithContainer(this.SelectedContainer.Id, this.SelectedContainer.Name, this.SelectedContainer.Description))
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
                ViewModel = _vmFactory.Dialog().WithConfirmation(
                    "Delete Container?",
                    $"Are you sure you want to delete ({this.SelectedContainer.Name})? All SKUs in this container will be un-assigned",
                    async () =>
                    {
                        var res = await _service.DeleteContainerAsync(new() { ContainerId = this.SelectedContainer.Id });
                        this.Messenger.ToastNotify($"Container Deleted. {res.UnassignedSkuTotal} SKU(s) un-assigned");
                        this.Messenger.Send(new ContainerDeletedMessage { Id = this.SelectedContainer.Id });
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
                ViewModel = _vmFactory.Dialog().WithContent(this.SelectedContainer.Name, _vmFactory.BrowseContainer().WithContainerId(this.SelectedContainer.Id))
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
                ViewModel = _vmFactory.Dialog().WithContent(this.SelectedContainer.Name, _vmFactory.ContainerText().WithText(text))
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
            this.Containers.Add(_vmFactory.Container().WithData(cont));
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

    void IRecipient<ContainerUpdatedMessage>.Receive(ContainerUpdatedMessage message)
    {
        var item = this.Containers.FirstOrDefault(c => c.Id == message.Container.Id);
        if (item != null)
        {
            item.WithData(message.Container);
        }
    }
}
