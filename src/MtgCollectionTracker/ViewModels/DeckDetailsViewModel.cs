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
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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

public partial class DeckDetailsViewModel : DialogContentViewModel, IMultiModeCardListBehaviorHost, IViewModelWithBusyState, IRecipient<CardSkuSplitMessage>, IRecipient<CardsRemovedFromDeckMessage>, IRecipient<DeckSideboardChangedMessage>, IRecipient<CardsAddedToDeckMessage>
{
    readonly ICollectionTrackingService _service;
    readonly IScryfallApiClient? _scryfallApiClient;

    readonly Func<DialogViewModel> _dialog;
    readonly Func<SplitCardSkuViewModel> _splitCardSku;
    readonly Func<SendCardsToContainerOrDeckViewModel> _sendToContainer;
    readonly Func<AddCardsViewModel> _addCards;
    readonly Func<AddExistingCardsToDeckViewModel> _addExistingCards;

    [ObservableProperty]
    private string _name;

    public DeckDetailsViewModel()
        : base(WeakReferenceMessenger.Default)
    {
        base.ThrowIfNotDesignMode();
        _service = new StubCollectionTrackingService();
        _dialog = () => new();
        _splitCardSku = () => new();
        _sendToContainer = () => new();
        _addCards = () => new();
        _addExistingCards = () => new();
        this.Behavior = new(this);
        this.Behavior.PropertyChanged += OnBehaviorPropertyChanged;
        this.IsActive = true;
        this.Name = "Test Deck";
    }

    public DeckDetailsViewModel(ICollectionTrackingService service,
                                Func<DialogViewModel> dialog,
                                Func<SplitCardSkuViewModel> splitCardSku,
                                Func<SendCardsToContainerOrDeckViewModel> sendToContainer,
                                Func<AddCardsViewModel> addCards,
                                Func<AddExistingCardsToDeckViewModel> addExistingCards,
                                IScryfallApiClient scryfallApiClient,
                                IMessenger messenger)
        : base(messenger)
    {
        _service = service;
        _scryfallApiClient = scryfallApiClient;
        _dialog = dialog;
        _splitCardSku = splitCardSku;
        _sendToContainer = sendToContainer;
        _addCards = addCards;
        _addExistingCards = addExistingCards;
        this.Behavior = new(this);
        this.Behavior.PropertyChanged += OnBehaviorPropertyChanged;
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
    [NotifyPropertyChangedFor(nameof(IsVisualSkuMode))]
    [NotifyPropertyChangedFor(nameof(SetPrimaryActionLabel))]
    [NotifyPropertyChangedFor(nameof(CanSetPrimaryAction))]
    [NotifyCanExecuteChangedFor(nameof(SetAsBannerCommand))]
    private DeckViewMode _mode = DeckViewMode.Text;

    IMessenger IViewModelWithBusyState.Messenger => this.Messenger;

    public bool IsSkuBasedMode => this.Mode == DeckViewMode.TableBySku || this.Mode == DeckViewMode.VisualBySku;

    public bool IsVisualSkuMode => this.Mode == DeckViewMode.VisualBySku;

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

    private Guid? _bannerCardId;

    partial void OnModeChanged(DeckViewMode value) => UpdateView(value);

    public bool IsVisualMode => this.Mode == DeckViewMode.VisualByCardName || this.Mode == DeckViewMode.VisualBySku;

    public bool IsTableMode => this.Mode == DeckViewMode.TableByCardName || this.Mode == DeckViewMode.TableBySku;

    public bool IsCommanderDeck => _origDeck?.IsCommander == true;

    public string SetPrimaryActionLabel => IsCommanderDeck ? "Set as Commander" : "Set as Banner";

    public bool CanSetPrimaryAction
    {
        get
        {
            if (!IsVisualSkuMode || !Behavior.HasOneSelectedItem)
                return false;

            if (!IsCommanderDeck)
                return true;

            return IsSelectedLegendaryCreature();
        }
    }

    [ObservableProperty]
    private bool _reportProxyUsage = false;

    partial void OnReportProxyUsageChanged(bool value)
    {
        this.DeckListText = _service.PrintDeckAsync(_origDeck.Id, new DeckPrintOptions(value), System.Threading.CancellationToken.None).GetAwaiter().GetResult();
    }

    private void UpdateView(DeckViewMode mode)
    {
        switch (mode)
        {
            case DeckViewMode.Text:
                this.DeckListText = _service.PrintDeckAsync(_origDeck.Id, new DeckPrintOptions(false), System.Threading.CancellationToken.None).GetAwaiter().GetResult();
                break;
            case DeckViewMode.VisualByCardName:
                UpdateVisual(_service, _origDeck, null, ref _mainDeckByCardName, this.MainDeck, ref _sideboardByCardName, this.Sideboard, c => c.CardName);
                break;
            case DeckViewMode.VisualBySku:
                UpdateVisual(_service, _origDeck, _bannerCardId, ref _mainDeckBySku, this.MainDeck, ref _sideboardBySku, this.Sideboard, c => c.SkuId);
                break;
            case DeckViewMode.TableByCardName:
                UpdateTable(_service, _origDeck, null, ref _mainDeckByCardName, ref _sideboardByCardName, this.TableList, c => c.CardName);
                break;
            case DeckViewMode.TableBySku:
                UpdateTable(_service, _origDeck, _bannerCardId, ref _mainDeckBySku, ref _sideboardBySku, this.TableList, c => c.SkuId);
                break;
        }
    }

    private List<CardVisualViewModel>? _mainDeckBySku;
    private List<CardVisualViewModel>? _sideboardBySku;
    private List<CardVisualViewModel>? _mainDeckByCardName;
    private List<CardVisualViewModel>? _sideboardByCardName;

    static void UpdateTable<T>(ICollectionTrackingService service,
                               DeckModel deck,
                               Guid? bannerCardId,
                               ref List<CardVisualViewModel>? maindeckBackingList,
                               ref List<CardVisualViewModel>? sideboardBackingList,
                               ObservableCollection<CardVisualViewModel> table,
                               Func<DeckCardModel, T> grouping)
    {
        InitBackingLists(service, deck, bannerCardId, ref maindeckBackingList, ref sideboardBackingList, grouping);
        table.Clear();
        foreach (var c in maindeckBackingList)
            table.Add(c);
        foreach (var c in sideboardBackingList)
            table.Add(c);
    }

    static void InitBackingLists<T>(ICollectionTrackingService service,
                                    DeckModel deck,
                                    Guid? bannerCardId,
                                    [NotNull] ref List<CardVisualViewModel>? maindeckBackingList,
                                    [NotNull] ref List<CardVisualViewModel>? sideboardBackingList,
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
                    IsProxy = CardListPrinter.IsProxyEdition(card.Edition),
                    Edition = card.Edition,
                    IsBanner = bannerCardId.HasValue && card.SkuId == bannerCardId.Value
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
                    IsProxy = CardListPrinter.IsProxyEdition(card.Edition),
                    Edition = card.Edition,
                    IsSideboard = true,
                    IsBanner = bannerCardId.HasValue && card.SkuId == bannerCardId.Value
                }.ApplyQuantities().ApplyScryfallMetadata(card);
                cm.SwitchToFront();
                sideboardBackingList.Add(cm);
            }
        }
    }

    static void UpdateVisual<T>(ICollectionTrackingService service,
                                DeckModel deck,
                                Guid? bannerCardId,
                                ref List<CardVisualViewModel>? maindeckBackingList,
                                ObservableCollection<CardVisualViewModel> maindeck,
                                ref List<CardVisualViewModel>? sideboardBackingList,
                                ObservableCollection<CardVisualViewModel> sideboard,
                                Func<DeckCardModel, T> grouping)
    {

        InitBackingLists(service, deck, bannerCardId, ref maindeckBackingList, ref sideboardBackingList, grouping);
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
        _bannerCardId = deck.BannerCardId;
        SetAsBannerCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(IsCommanderDeck));
        OnPropertyChanged(nameof(SetPrimaryActionLabel));
        OnPropertyChanged(nameof(CanSetPrimaryAction));

        this.MainDeckSize = _origDeck.MainDeck.Count;
        this.SideboardSize = _origDeck.Sideboard.Count;

        this.UpdateView(this.Mode);

        return this;
    }

    [RelayCommand(CanExecute = nameof(CanSetPrimaryAction))]
    private async Task SetAsBanner()
    {
        if (Behavior.SelectedItems.Count == 1)
        {
            var selected = Behavior.SelectedItems[0];
            using (((IViewModelWithBusyState)this).StartBusyState())
            {
                DeckSummaryModel updatedDeck;
                if (IsCommanderDeck)
                {
                    _bannerCardId = selected.Id;
                    _origDeck.BannerCardId = selected.Id;

                    await _service.SetDeckBannerAsync(_origDeck.Id, selected.Id, System.Threading.CancellationToken.None);
                    updatedDeck = await _service.SetDeckCommanderAsync(_origDeck.Id, selected.Id, System.Threading.CancellationToken.None);

                    var selectedCommander = _origDeck.MainDeck.FirstOrDefault(c => c.SkuId == selected.Id)
                        ?? _origDeck.Sideboard.FirstOrDefault(c => c.SkuId == selected.Id);
                    _origDeck.Commander = selectedCommander;
                }
                else
                {
                    // Toggle: if this card is already the banner, clear it; otherwise set it
                    var newBannerId = selected.Id == _bannerCardId ? (Guid?)null : selected.Id;
                    updatedDeck = await _service.SetDeckBannerAsync(_origDeck.Id, newBannerId, System.Threading.CancellationToken.None);
                    _bannerCardId = newBannerId;
                    _origDeck.BannerCardId = newBannerId;
                }

                // Update IsBanner flags on all loaded card view models
                UpdateBannerFlags();
                // Notify the deck collection view to update the banner image
                Messenger.Send(new DeckUpdatedMessage(updatedDeck));
            }
        }
    }

    private void UpdateBannerFlags()
    {
        var allCards = (_mainDeckBySku ?? [])
            .Concat(_sideboardBySku ?? [])
            .Concat(_mainDeckByCardName ?? [])
            .Concat(_sideboardByCardName ?? []);
        foreach (var card in allCards)
        {
            card.IsBanner = _bannerCardId.HasValue && card.Id == _bannerCardId.Value;
        }
    }

    [RelayCommand]
    private void SplitSelectedSku()
    {
        if (Behavior.IsItemSplittable)
        {
            var selected = Behavior.SelectedItems[0];
            var vm = _splitCardSku();
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
                ViewModel = _dialog().WithContent("Split Card SKU", vm)
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
                ViewModel = _dialog().WithContent("Send Cards To Deck or Container", _sendToContainer().WithCards(Behavior.SelectedItems.ToList()))
            });
        }
    }

    [RelayCommand]
    private void AddNewCards()
    {
        var vm = _addCards().WithTargetDeck(_origDeck.Id, _origDeck.Name);
        Messenger.Send(new OpenDialogMessage
        {
            DrawerWidth = 800,
            ViewModel = _dialog().WithContent($"Add New Cards to Deck: {_origDeck.Name}", vm, canClose: false)
        });
    }

    [RelayCommand]
    private void AddExistingCards()
    {
        var vm = _addExistingCards().WithDeck(_origDeck.Id, _origDeck.Name);
        Messenger.Send(new OpenDialogMessage
        {
            DrawerWidth = 800,
            ViewModel = _dialog().WithContent($"Add Existing Cards to Deck: {_origDeck.Name}", vm)
        });
    }

    [RelayCommand]
    private async Task UpdateSkuMetadata()
    {
        if (Behavior.SelectedItems.Count > 0 && _scryfallApiClient != null)
        {
            using (((IViewModelWithBusyState)this).StartBusyState())
            {
                var ids = Behavior.SelectedItems.Select(c => c.Id).ToList();
                var callback = new UpdateCardMetadataProgressCallback
                {
                    OnProgress = (processed, total) =>
                    {
                        Messenger.ToastNotify($"Updated metadata for {processed} of {total} sku(s)", Avalonia.Controls.Notifications.NotificationType.Success);
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
                Messenger.ToastNotify("Metadata updated", Avalonia.Controls.Notifications.NotificationType.Success);
            }
        }
    }

    public void HandleBusyChanged(bool oldValue, bool newValue) { }

    private void OnBehaviorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MultiModeCardListBehavior<CardVisualViewModel>.HasOneSelectedItem)
            || e.PropertyName == nameof(MultiModeCardListBehavior<CardVisualViewModel>.SelectedCardType)
            || e.PropertyName == nameof(MultiModeCardListBehavior<CardVisualViewModel>.IsBusy))
        {
            SetAsBannerCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(CanSetPrimaryAction));
        }
    }

    private bool IsSelectedLegendaryCreature()
    {
        var type = Behavior.SelectedCardType;
        return !string.IsNullOrWhiteSpace(type)
            && type.Contains("Legendary", StringComparison.OrdinalIgnoreCase)
            && type.Contains("Creature", StringComparison.OrdinalIgnoreCase);
    }

    private void RefreshDeckView()
    {
        this.MainDeckSize = _origDeck.MainDeck.Count;
        this.SideboardSize = _origDeck.Sideboard.Count;
        _mainDeckByCardName = _mainDeckBySku = _sideboardByCardName = _sideboardBySku = null;
        UpdateView(this.Mode);
    }

    void IRecipient<CardSkuSplitMessage>.Receive(CardSkuSplitMessage message)
    {
        if (message.DeckId == _origDeck.Id)
        {
            int mdm = UpdateList(_origDeck.MainDeck, message.SplitSkuId, message.NewSkuId, message.Quantity);
            int sbm = UpdateList(_origDeck.Sideboard, message.SplitSkuId, message.NewSkuId, message.Quantity);

            RefreshDeckView();

            static int UpdateList(List<DeckCardModel> list, Guid skuId, Guid newSkuId, int quantity)
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

            RefreshDeckView();
        }
    }

    void IRecipient<DeckSideboardChangedMessage>.Receive(DeckSideboardChangedMessage message)
    {
        if (message.DeckId == _origDeck.Id)
        {
            if (message.IsSideboard)
            {
                // Move matching cards from main deck to sideboard
                var toMove = _origDeck.MainDeck.Where(c => message.SkuIds.Contains(c.SkuId)).ToList();
                foreach (var card in toMove)
                {
                    _origDeck.MainDeck.Remove(card);
                    _origDeck.Sideboard.Add(card);
                }
            }
            else
            {
                // Move matching cards from sideboard to main deck
                var toMove = _origDeck.Sideboard.Where(c => message.SkuIds.Contains(c.SkuId)).ToList();
                foreach (var card in toMove)
                {
                    _origDeck.Sideboard.Remove(card);
                    _origDeck.MainDeck.Add(card);
                }
            }

            RefreshDeckView();
        }
    }

    internal void DeckListCopiedToClipboard() => Messenger.ToastNotify("Deck list copied to clipboard", Avalonia.Controls.Notifications.NotificationType.Information);

    void IRecipient<CardsAddedToDeckMessage>.Receive(CardsAddedToDeckMessage message)
    {
        if (message.DeckId == _origDeck.Id)
        {
            ReloadDeckAsync().ContinueWith(t =>
            {
                if (t.IsFaulted)
                    Messenger.ToastNotify("Failed to refresh deck view", Avalonia.Controls.Notifications.NotificationType.Error);
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
    }

    private async Task ReloadDeckAsync()
    {
        var deck = await _service.GetDeckAsync(_origDeck.Id, _scryfallApiClient, CancellationToken.None);
        _origDeck = deck;
        RefreshDeckView();
    }
}
