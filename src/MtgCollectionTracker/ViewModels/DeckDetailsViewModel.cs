using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services;
using MtgCollectionTracker.Services.Contracts;
using MtgCollectionTracker.Services.Messaging;
using MtgCollectionTracker.Services.Stubs;
using ScryfallApi.Client;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MtgCollectionTracker.ViewModels;

public enum DeckViewMode
{
    /// <summary>
    /// View the decklist in text mode
    /// </summary>
    Text,
    /// <summary>
    /// View the decklist as a table, by SKUs
    /// </summary>
    TableBySku,
    /// <summary>
    /// View the decklist visually, by SKUs
    /// </summary>
    VisualBySku,
    /// <summary>
    /// View the decklist as a table, by card name
    /// </summary>
    TableByCardName,
    /// <summary>
    /// View the decklist visually, by card name
    /// </summary>
    VisualByCardName
}

public partial class DeckDetailsViewModel : DialogContentViewModel, IMultiModeCardListBehaviorHost, IViewModelWithBusyState, IRecipient<CardSkuSplitMessage>, IRecipient<CardsRemovedFromDeckMessage>
{
    readonly ICollectionTrackingService _service;
    readonly IViewModelFactory _vmFactory;
    readonly IScryfallApiClient? _scryfallApiClient;

    [ObservableProperty]
    private string _name;

    public DeckDetailsViewModel()
        : base(WeakReferenceMessenger.Default)
    {
        base.ThrowIfNotDesignMode();
        _service = new StubCollectionTrackingService();
        _vmFactory = new StubViewModelFactory();
        this.Behavior = new(this);
        this.IsActive = true;
        this.Name = "Test Deck";
    }

    public DeckDetailsViewModel(ICollectionTrackingService service,
                                IViewModelFactory vmFactory,
                                IScryfallApiClient scryfallApiClient,
                                IMessenger messenger)
        : base(messenger)
    {
        _service = service;
        _vmFactory = vmFactory;
        _scryfallApiClient = scryfallApiClient;
        this.Behavior = new(this);
        this.IsActive = true;
    }

    bool IViewModelWithBusyState.IsBusy
    {
        get => Behavior.IsBusy;
        set => Behavior.IsBusy = value;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTableMode))]
    [NotifyPropertyChangedFor(nameof(IsVisualMode))]
    [NotifyPropertyChangedFor(nameof(IsSkuBasedMode))]
    private DeckViewMode _mode = DeckViewMode.Text;

    public bool IsSkuBasedMode => this.Mode == DeckViewMode.TableBySku || this.Mode == DeckViewMode.VisualBySku;

    // For table view
    public MultiModeCardListBehavior<CardVisualViewModel> Behavior { get; }

    public ObservableCollection<CardVisualViewModel> TableList { get; } = new();

    // For text view

    [ObservableProperty]
    private string _deckListText;

    // For visual view

    public ObservableCollection<CardVisualViewModel> MainDeck { get; } = new();

    public ObservableCollection<CardVisualViewModel> Sideboard { get; } = new();

    // Misc properties

    [ObservableProperty]
    private int _mainDeckSize;

    [ObservableProperty]
    private int _sideboardSize;

    private DeckModel _origDeck;

    partial void OnModeChanged(DeckViewMode value) => UpdateView(value);

    public bool IsVisualMode => this.Mode == DeckViewMode.VisualByCardName || this.Mode == DeckViewMode.VisualBySku;

    public bool IsTableMode => this.Mode == DeckViewMode.TableByCardName || this.Mode == DeckViewMode.TableBySku;

    [ObservableProperty]
    private bool _reportProxyUsage = false;

    private void UpdateView(DeckViewMode mode)
    {
        switch (mode)
        {
            case DeckViewMode.Text:
                var text = new StringBuilder();
                DeckPrinter.Print(_origDeck.Name, _origDeck.Format, _origDeck.GetCards(), s => text.AppendLine(s), this.ReportProxyUsage);
                this.DeckListText = text.ToString();
                break;
            case DeckViewMode.VisualByCardName:
                UpdateVisual(_service, _origDeck, ref _mainDeckByCardName, this.MainDeck, ref _sideboardByCardName, this.Sideboard, c => c.CardName);
                break;
            case DeckViewMode.VisualBySku:
                UpdateVisual(_service, _origDeck, ref _mainDeckBySku, this.MainDeck, ref _sideboardBySku, this.Sideboard, c => c.SkuId);
                break;
            case DeckViewMode.TableByCardName:
                UpdateTable(_service, _origDeck, ref _mainDeckByCardName, ref _sideboardByCardName, this.TableList, c => c.CardName);
                break;
            case DeckViewMode.TableBySku:
                UpdateTable(_service, _origDeck, ref _mainDeckBySku, ref _sideboardBySku, this.TableList, c => c.SkuId);
                break;
        }
    }

    private List<CardVisualViewModel>? _mainDeckBySku;
    private List<CardVisualViewModel>? _sideboardBySku;
    private List<CardVisualViewModel>? _mainDeckByCardName;
    private List<CardVisualViewModel>? _sideboardByCardName;

    static void UpdateTable<T>(ICollectionTrackingService service,
                               DeckModel deck,
                               ref List<CardVisualViewModel> maindeckBackingList,
                               ref List<CardVisualViewModel> sideboardBackingList,
                               ObservableCollection<CardVisualViewModel> table,
                               Func<DeckCardModel, T> grouping)
    {
        InitBackingLists(service, deck, ref maindeckBackingList, ref sideboardBackingList, grouping);
        table.Clear();
        foreach (var c in maindeckBackingList)
            table.Add(c);
        foreach (var c in sideboardBackingList)
            table.Add(c);
    }

    static void InitBackingLists<T>(ICollectionTrackingService service,
                                    DeckModel deck,
                                    ref List<CardVisualViewModel> maindeckBackingList,
                                    ref List<CardVisualViewModel> sideboardBackingList,
                                    Func<DeckCardModel, T> grouping)
    {
        // Init the backing list only once
        if (maindeckBackingList == null)
        {
            maindeckBackingList = new();
            var md = new List<CardVisualViewModel>();
            foreach (var grp in deck.MainDeck.GroupBy(grouping))
            {
                var card = grp.First();
                var cm = new CardVisualViewModel(service)
                {
                    IsGrouped = true,
                    Id = card.SkuId,
                    ScryfallId = card.ScryfallId,
                    IsDoubleFaced = card.IsDoubleFaced,
                    Quantity = grp.Count(),
                    CardName = card.CardName,
                    Type = card.Type,
                    IsLand = card.IsLand,
                    IsProxy = DeckPrinter.IsProxyEdition(card.Edition),
                    Edition = card.Edition,
                }.ApplyQuantities().ApplyScryfallMetadata(card);
                cm.SwitchToFront();
                md.Add(cm);
            }
            // Non-lands before lands
            foreach (var c in md.OrderBy(m => m.IsLand).ThenBy(m => m.Type))
                maindeckBackingList.Add(c);
        }
        // Init the backing list only once
        if (sideboardBackingList == null)
        {
            sideboardBackingList = new();
            foreach (var grp in deck.Sideboard.GroupBy(grouping))
            {
                var card = grp.First();
                var cm = new CardVisualViewModel(service)
                {
                    IsGrouped = true,
                    Id = card.SkuId,
                    ScryfallId = card.ScryfallId,
                    IsDoubleFaced = card.IsDoubleFaced,
                    Quantity = grp.Count(),
                    CardName = card.CardName,
                    Type = card.Type,
                    IsLand = card.IsLand,
                    IsProxy = DeckPrinter.IsProxyEdition(card.Edition),
                    Edition = card.Edition,
                    IsSideboard = true
                }.ApplyQuantities().ApplyScryfallMetadata(card);
                cm.SwitchToFront();
                sideboardBackingList.Add(cm);
            }
        }
    }

    static void UpdateVisual<T>(ICollectionTrackingService service,
                                DeckModel deck,
                                ref List<CardVisualViewModel> maindeckBackingList,
                                ObservableCollection<CardVisualViewModel> maindeck,
                                ref List<CardVisualViewModel> sideboardBackingList,
                                ObservableCollection<CardVisualViewModel> sideboard,
                                Func<DeckCardModel, T> grouping)
    {

        InitBackingLists(service, deck, ref maindeckBackingList, ref sideboardBackingList, grouping);
        // Sync backing list to their respective ObservableCollection
        maindeck.Clear();
        sideboard.Clear();
        foreach (var c in maindeckBackingList)
            maindeck.Add(c);
        foreach (var c in sideboardBackingList)
            sideboard.Add(c);
    }

    public DeckDetailsViewModel WithDeck(DeckModel deck)
    {
        this.Name = deck.Name;

        _origDeck = deck;

        this.MainDeckSize = _origDeck.MainDeck.Count;
        this.SideboardSize = _origDeck.Sideboard.Count;

        this.UpdateView(this.Mode);

        return this;
    }

    [RelayCommand]
    private void SplitSelectedSku()
    {
        if (Behavior.IsItemSplittable)
        {
            var selected = Behavior.SelectedItems[0];
            var vm = _vmFactory.SplitCardSku();
            vm.CardSkuId = selected.Id;
            if (selected.ProxyQty > 1)
            {
                vm.CurrentQuantity = selected.ProxyQty;
            }
            else if (selected.RealQty > 1)
            {
                vm.CurrentQuantity = selected.RealQty;
            }
            if (vm.CurrentQuantity == 0)
                return;

            vm.SplitQuantity = vm.SplitMin;
            Messenger.Send(new OpenDialogMessage
            {
                DrawerWidth = 300,
                ViewModel = _vmFactory.Drawer().WithContent("Split Card SKU", vm)
            });
        }
    }

    [RelayCommand]
    private void SendSkusToContainer()
    {
        if (Behavior.SelectedItems.Count > 0)
        {
            Messenger.Send(new OpenDialogMessage
            {
                DrawerWidth = 800,
                ViewModel = _vmFactory.Drawer().WithContent("Send Cards To Deck or Container", _vmFactory.SendCardsToContainer().WithCards(Behavior.SelectedItems.ToList()))
            });
        }
    }

    [RelayCommand]
    private async Task UpdateSkuMetadata()
    {
        if (Behavior.SelectedItems.Count > 0)
        {
            using (((IViewModelWithBusyState)this).StartBusyState())
            {
                var ids = Behavior.SelectedItems.Select(c => c.Id).ToList();
                var callback = new UpdateCardMetadataProgressCallback
                {
                    OnProgress = (processed, total) =>
                    {
                        Messenger.ToastNotify($"Updated metadata for {processed} of {total} sku(s)");
                    }
                };
                // FIXME: With multiple selections, it seems in general one needs to invoke this twice for the new
                // metadata to stick. I currently don't know why this is the case
                var updatedSkus = await _service.UpdateCardMetadataAsync(ids, _scryfallApiClient, callback, CancellationToken.None);
                /*
                int updated = 0;
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
                */
                Messenger.ToastNotify("Metadata updated");
            }
        }
    }

    public void HandleBusyChanged(bool oldValue, bool newValue) { }

    void IRecipient<CardSkuSplitMessage>.Receive(CardSkuSplitMessage message)
    {
        if (message.DeckId == _origDeck.Id)
        {
            int mdm = UpdateList(_origDeck.MainDeck, message.SplitSkuId, message.NewSkuId, message.Quantity);
            int sbm = UpdateList(_origDeck.Sideboard, message.SplitSkuId, message.NewSkuId, message.Quantity);

            this.MainDeckSize = _origDeck.MainDeck.Count;
            this.SideboardSize = _origDeck.Sideboard.Count;

            // Bit nuclear, but will do the job for now
            _mainDeckByCardName = _mainDeckBySku = _sideboardByCardName = _sideboardBySku = null;
            UpdateView(this.Mode);

            static int UpdateList(List<DeckCardModel> list, int skuId, int newSkuId, int quantity)
            {
                int moved = 0;
                var skus = list.FindAll(c => c.SkuId == skuId);
                if (skus.Count > 0)
                {
                    Debug.Assert(quantity > 0 && quantity < skus.Count);
                    for (int i = 0; i < quantity; i++)
                    {
                        var newCard = skus[0].WithSkuId(newSkuId);
                        list.Remove(skus[i]);
                        list.Add(newCard);
                        moved++;
                    }
                }
                return moved;
            }
        }
    }

    void IRecipient<CardsRemovedFromDeckMessage>.Receive(CardsRemovedFromDeckMessage message)
    {
        if (message.DeckId == _origDeck.Id)
        {
            _origDeck.MainDeck.RemoveAll(c => message.SkuIds.Contains(c.SkuId));
            _origDeck.Sideboard.RemoveAll(c => message.SkuIds.Contains(c.SkuId));

            this.MainDeckSize = _origDeck.MainDeck.Count;
            this.SideboardSize = _origDeck.Sideboard.Count;

            // Bit nuclear, but will do the job for now
            _mainDeckByCardName = _mainDeckBySku = _sideboardByCardName = _sideboardBySku = null;
            UpdateView(this.Mode);
        }
    }
}
