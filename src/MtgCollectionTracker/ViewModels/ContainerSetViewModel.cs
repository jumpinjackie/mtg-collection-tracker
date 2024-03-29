﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services.Contracts;
using MtgCollectionTracker.Services.Stubs;
using System.Collections.ObjectModel;

namespace MtgCollectionTracker.ViewModels;

public partial class ContainerSetViewModel : RecipientViewModelBase
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
    }

    public ObservableCollection<ContainerViewModel> Containers { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedContainer))]
    private ContainerViewModel? _selectedContainer;

    public bool HasSelectedContainer => this.SelectedContainer != null;

    [RelayCommand]
    private void AddContainer()
    {

    }

    [RelayCommand]
    private void ViewContainer()
    {

    }
}
