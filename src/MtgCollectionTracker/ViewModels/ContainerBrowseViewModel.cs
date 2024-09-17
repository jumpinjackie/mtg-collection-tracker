using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services;
using MtgCollectionTracker.Services.Contracts;
using MtgCollectionTracker.Services.Messaging;
using MtgCollectionTracker.Services.Stubs;
using ScryfallApi.Client;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MtgCollectionTracker.ViewModels;

public partial class ContainerBrowseViewModel : DrawerContentViewModel, IViewModelWithBusyState
{
    readonly ICollectionTrackingService _service;
    readonly IScryfallApiClient? _scryfallApiClient;
    readonly IViewModelFactory _vmFactory;

    public ContainerBrowseViewModel()
    {
        this.ThrowIfNotDesignMode();
        _service = new StubCollectionTrackingService();
        _vmFactory = new StubViewModelFactory();
        this.SelectedCardSkus.CollectionChanged += SelectedCardSkus_CollectionChanged;
        this.IsActive = true;
    }

    public ContainerBrowseViewModel(
        ICollectionTrackingService service,
        IScryfallApiClient scryfallApiClient,
        IViewModelFactory vmFactory,
        IMessenger messenger)
        : base(messenger)
    {
        _service = service;
        _scryfallApiClient = scryfallApiClient;
        _vmFactory = vmFactory;
        this.SelectedCardSkus.CollectionChanged += SelectedCardSkus_CollectionChanged;
        this.IsActive = true;
    }
    private void SelectedCardSkus_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        this.HasMultipleSelectedCardSkus = this.SelectedCardSkus.Count > 1;
        this.HasSelectedCardSku = this.SelectedCardSkus.Count == 1;
        this.HasAtLeastOneSelectedCardSku = this.SelectedCardSkus.Count > 0;
    }

    private int? _containerId;

    [ObservableProperty]
    private int _pageNumber = -1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreviousEnabled))]
    private bool _canGoPrevious;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NextEnabled))]
    private bool _canGoNext;

    [ObservableProperty]
    private bool _showOnlyMissingMetadata;

    partial void OnShowOnlyMissingMetadataChanged(bool value)
    {
        FetchPage(this.PageNumber);
    }

    private void FetchPage(int oneBasedPageNumber)
    {
        using (((IViewModelWithBusyState)this).StartBusyState())
        {
            var page = _service.GetCardsForContainer(_containerId.Value, new()
            {
                ShowOnlyMissingMetadata = this.ShowOnlyMissingMetadata,
                PageNumber = oneBasedPageNumber - 1
            });
            this.SearchResults.Clear();
            foreach (var sku in page.Items)
            {
                this.SearchResults.Add(_vmFactory.CardSku().WithData(sku));
            }
            this.HasNoResults = this.SearchResults.Count == 0;
            var from = Math.Max(page.PageNumber, 0) * page.PageSize;
            var to = Math.Min((page.PageNumber + 1) * page.PageSize, page.Total);
            this.PageSummary = $"Viewing {from} - {to} of {page.Total} skus";
            this.CanGoPrevious = (oneBasedPageNumber - 1) > 0;
            this.CanGoNext = (this.SearchResults.Count == page.PageSize);
        }
    }

    partial void OnPageNumberChanged(int oldValue, int newValue)
    {
        if (oldValue != newValue && newValue > 0 && _containerId.HasValue)
        {
            FetchPage(newValue);
        }
    }

    [RelayCommand]
    private void NextPage()
    {
        this.PageNumber++;
    }

    [RelayCommand]
    private void PreviousPage()
    {
        this.PageNumber--;
    }

    public ContainerBrowseViewModel WithContainerId(int containerId)
    {
        _containerId = containerId;
        this.PageNumber = 1;
        return this;
    }

    public bool NextEnabled => this.CanGoNext && !this.IsBusy;

    public bool PreviousEnabled => this.CanGoPrevious && !this.IsBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSendSkusToContainer))]
    [NotifyPropertyChangedFor(nameof(CanSendSkusToDeck))]
    [NotifyPropertyChangedFor(nameof(CanUpdateMetadata))]
    [NotifyPropertyChangedFor(nameof(CanCombineCardSkus))]
    [NotifyPropertyChangedFor(nameof(CanSplitCardSku))]
    [NotifyPropertyChangedFor(nameof(PreviousEnabled))]
    [NotifyPropertyChangedFor(nameof(NextEnabled))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSplitCardSku))]
    private bool _hasSelectedCardSku;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCombineCardSkus))]
    private bool _hasMultipleSelectedCardSkus;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSendSkusToContainer))]
    [NotifyPropertyChangedFor(nameof(CanSendSkusToDeck))]
    [NotifyPropertyChangedFor(nameof(CanUpdateMetadata))]
    private bool _hasAtLeastOneSelectedCardSku;

    [ObservableProperty]
    private string? _containerSummary;

    [ObservableProperty]
    private string? _pageSummary;

    [ObservableProperty]
    private bool _showSearchResults;

    [ObservableProperty]
    private bool _hasNoResults;

    public bool CanCombineCardSkus => !this.IsBusy && this.HasMultipleSelectedCardSkus;
    public bool CanSplitCardSku => !this.IsBusy && this.HasSelectedCardSku;
    public bool CanSendSkusToContainer => !this.IsBusy && this.HasAtLeastOneSelectedCardSku;
    public bool CanSendSkusToDeck => !this.IsBusy && this.HasAtLeastOneSelectedCardSku;
    public bool CanUpdateMetadata => !this.IsBusy && this.HasAtLeastOneSelectedCardSku;

    public ObservableCollection<CardSkuItemViewModel> SearchResults { get; } = new();

    public ObservableCollection<CardSkuItemViewModel> SelectedCardSkus { get; } = new();

    [RelayCommand]
    private void AddSkus()
    {
        Messenger.Send(new OpenDrawerMessage
        {
            DrawerWidth = 800,
            ViewModel = _vmFactory.Drawer().WithContent("Add Cards", _vmFactory.AddCards())
        });
    }

    [RelayCommand]
    private void ViewSelectedSku()
    {

    }

    [RelayCommand]
    private void SendSkusToContainer()
    {
        if (this.SelectedCardSkus.Count > 0)
        {
            Messenger.Send(new OpenDrawerMessage
            {
                DrawerWidth = 800,
                ViewModel = _vmFactory.Drawer().WithContent("Send Cards To Deck or Container", _vmFactory.SendCardsToContainer(this.SelectedCardSkus))
            });
        }
    }

    [RelayCommand]
    private async Task UpdateSkuMetadata()
    {
        if (this.SelectedCardSkus.Count > 0)
        {
            using (((IViewModelWithBusyState)this).StartBusyState())
            {
                int updated = 0;
                var ids = this.SelectedCardSkus.Select(c => c.Id);
                var updatedSkus = await _service.UpdateCardMetadataAsync(ids, _scryfallApiClient, CancellationToken.None);
                foreach (var sku in updatedSkus)
                {
                    var skuM = this.SearchResults.FirstOrDefault(c => c.Id == sku.Id);
                    if (skuM != null)
                    {
                        skuM.WithData(sku);
                        updated++;
                    }
                }
                if (updated > 0)
                {
                    Messenger.ToastNotify($"Metadata updated for {updated} sku(s)");
                }
            }
        }
    }
}