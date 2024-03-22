using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Data;
using MtgCollectionTracker.Services.Messaging;
using MtgCollectionTracker.Services.Stubs;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MtgCollectionTracker.ViewModels;

public partial class AddCardSkuViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    [NotifyCanExecuteChangedFor(nameof(AddCardsCommand))]
    private int _qty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    [NotifyCanExecuteChangedFor(nameof(AddCardsCommand))]
    private string _cardName;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    [NotifyCanExecuteChangedFor(nameof(AddCardsCommand))]
    private string _edition;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    [NotifyCanExecuteChangedFor(nameof(AddCardsCommand))]
    private string? _language;

    [ObservableProperty]
    private bool _isFoil;

    [ObservableProperty]
    private bool _isLand;

    [ObservableProperty]
    private CardCondition? _condition;

    [ObservableProperty]
    private string? _comments;

    public bool IsValid => this.Qty > 0 && !string.IsNullOrEmpty(this.CardName) && !string.IsNullOrEmpty(this.Edition);

    /// <summary>
    /// Reference copy of the root view model command so we can re-evaluate executability from this item
    /// </summary>
    public required IAsyncRelayCommand AddCardsCommand { get; set; }
}

public partial class AddCardsViewModel : DrawerContentViewModel
{
    readonly IMessenger _messenger;
    readonly ICollectionTrackingService _service;

    public AddCardsViewModel()
    {
        base.ThrowIfNotDesignMode();
        _messenger = WeakReferenceMessenger.Default;
        _service = new StubCollectionTrackingService();

        this.Cards.Add(new() { AddCardsCommand = this.AddCardsCommand, Qty = 1, CardName = "Black Lotus", Edition = "LEB" });
        this.Cards.Add(new() { AddCardsCommand = this.AddCardsCommand, Qty = 1, CardName = "Mox Pearl", Edition = "LEB" });
        this.Cards.Add(new() { AddCardsCommand = this.AddCardsCommand, Qty = 1, CardName = "Mox Emerald", Edition = "LEB" });
        this.Cards.Add(new() { AddCardsCommand = this.AddCardsCommand, Qty = 1, CardName = "Mox Ruby", Edition = "LEB" });
        this.Cards.Add(new() { AddCardsCommand = this.AddCardsCommand, Qty = 1, CardName = "Mox Jet", Edition = "LEB" });
        this.Cards.Add(new() { AddCardsCommand = this.AddCardsCommand, Qty = 1, CardName = "Mox Sapphire", Edition = "LEB" });
        this.Cards.Add(new() { AddCardsCommand = this.AddCardsCommand, Qty = 1, CardName = "Ancestral Recall", Edition = "LEB" });
        this.Cards.Add(new() { AddCardsCommand = this.AddCardsCommand, Qty = 1, CardName = "Time Walk", Edition = "LEB" });
        this.Cards.Add(new() { AddCardsCommand = this.AddCardsCommand, Qty = 1, CardName = "Timetwister", Edition = "LEB" });
    }

    public AddCardsViewModel(IMessenger messenger, ICollectionTrackingService service)
    {
        _messenger = messenger;
        _service = service;
    }

    public ObservableCollection<AddCardSkuViewModel> Cards { get; } = new();

    [RelayCommand]
    private void AddRow()
    {
        Cards.Add(new AddCardSkuViewModel { AddCardsCommand = this.AddCardsCommand, Qty = 1, CardName = "", Edition = "" });
        AddCardsCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void RemoveCard(AddCardSkuViewModel item)
    {
        this.Cards.Remove(item);
        AddCardsCommand.NotifyCanExecuteChanged();
    }

    private bool CanAddCards() => Cards.Count > 0 && Cards.All(c => c.IsValid);

    [ObservableProperty]
    private int? _deckId;

    [ObservableProperty]
    private int? _containerId;

    [RelayCommand(CanExecute = nameof(CanAddCards))]
    private async Task AddCards()
    {
        var adds = this.Cards.Select(c => new AddToDeckOrContainerInputModel
        {
            CardName = c.CardName,
            Comments = c.Comments,
            Condition = c.Condition,
            IsFoil = c.IsFoil,
            IsLand = c.IsLand,
            Language = c.Language,
            Quantity = c.Qty,
            Edition = c.Edition
        });
        var (total, proxyTotal, rows) = await _service.AddMultipleToContainerOrDeckAsync(this.ContainerId, this.DeckId, adds);
        _messenger.Send(new CardsAddedMessage { CardsTotal = total, ProxyTotal = proxyTotal, SkuTotal = rows });
        _messenger.Send(new CloseDrawerMessage());
    }

    [RelayCommand]
    private void Cancel()
    {
        _messenger.Send(new CloseDrawerMessage());
    }
}
