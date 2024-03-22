using CommunityToolkit.Mvvm.ComponentModel;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services.Contracts;
using MtgCollectionTracker.Services.Stubs;
using System.Collections.ObjectModel;

namespace MtgCollectionTracker.ViewModels;

public partial class ContainerSetViewModel : ViewModelBase
{
    readonly IViewModelFactory _vmFactory;
    readonly ICollectionTrackingService _service;

    public ContainerSetViewModel()
    {
        base.ThrowIfNotDesignMode();
        _vmFactory = new StubViewModelFactory();
        _service = new StubCollectionTrackingService();
    }

    public ContainerSetViewModel(IViewModelFactory vmFactory, ICollectionTrackingService service)
    {
        _vmFactory = vmFactory;
        _service = service;
    }

    internal void LoadContainers()
    {
        this.Containers.Clear();
        var containers = _service.GetContainers();
        foreach (var cont in containers)
        {
            this.Containers.Add(_vmFactory.Container().WithData(cont));
        }
    }

    public ObservableCollection<ContainerViewModel> Containers { get; } = new();

    [ObservableProperty]
    private ContainerViewModel? _selectedContainer;
}
