using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services.Contracts;
using MtgCollectionTracker.Services.Stubs;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace MtgCollectionTracker.ViewModels;

public partial class LoanViewModel : DialogContentViewModel, IViewModelWithBusyState
{
    readonly ICollectionTrackingService _service;
    readonly Func<CardSkuItemViewModel> _cardSku;

    public LoanViewModel(ICollectionTrackingService service, Func<CardSkuItemViewModel> cardSku, IMessenger messenger)
        : base(messenger)
    {
        _service = service;
        _cardSku = cardSku;
    }

    public LoanViewModel()
        : base(WeakReferenceMessenger.Default)
    {
        this.ThrowIfNotDesignMode();
        _service = new StubCollectionTrackingService();
        _cardSku = () => new();
    }

    public int Id { get; set; }

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _fromDecksOrContainers = string.Empty;

    [ObservableProperty]
    private string _toDeck = string.Empty;

    public int ToDeckId { get; set; }

    [ObservableProperty]
    private string _searchText = string.Empty;

    public ObservableCollection<CardSkuItemViewModel> SearchResults { get; } = new();

    public ObservableCollection<CardSkuItemViewModel> DeckCards { get; } = new();

    public ObservableCollection<CardSkuItemViewModel> LoanedOutCards { get; } = new();

    public ObservableCollection<CardSkuItemViewModel> CardsReplaced { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoanCardsCommand))]
    private CardSkuItemViewModel? _selectedSearchResult;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(TakeCardFromDeckCommand))]
    private CardSkuItemViewModel? _selectedDeckCard;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ReturnCardToDeckCommand))]
    private CardSkuItemViewModel? _selectedReplaceCard;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ReturnLoanedCardCommand))]
    private CardSkuItemViewModel? _selectedLoanedCard;

    IMessenger IViewModelWithBusyState.Messenger => this.Messenger;

    [ObservableProperty]
    private bool _isBusy;

    [RelayCommand]
    private async Task Search(CancellationToken cancel)
    {
        var hasMinSearchParams = !string.IsNullOrWhiteSpace(this.SearchText); // || this.SelectedTags.Count > 0 || this.UnParented || this.MissingMetadata;
        if (!hasMinSearchParams)
            return;

        using (((IViewModelWithBusyState)this).StartBusyState())
        {
            await Task.Delay(500);
            var cards = _service.GetCards(new Core.Model.CardQueryModel
            {
                SearchFilter = this.SearchText,
                NotInDeckIds = [this.ToDeckId],
                NotLoanedOut = true
                /*
                Tags = this.SelectedTags.Count > 0 ? this.SelectedTags : null,
                NoProxies = this.NoProxies,
                NotInDecks = this.NotInDecks,
                UnParented = this.UnParented,
                MissingMetadata = this.MissingMetadata,
                IncludeScryfallMetadata = true
                */
            });
            this.SearchResults.Clear();
            foreach (var sku in cards)
            {
                this.SearchResults.Add(_cardSku().WithData(sku));
            }
            //this.HasNoResults = !this.ShowFirstTimeMessage && this.SearchResults.Count == 0;
        }
    }

    private bool CanLoanCards() => this.SelectedSearchResult != null;

    [RelayCommand(CanExecute = nameof(CanLoanCards))]
    private void LoanCards()
    {
        if (this.SelectedSearchResult != null && !this.LoanedOutCards.Contains(this.SelectedSearchResult))
        {
            this.LoanedOutCards.Add(this.SelectedSearchResult);
            this.SelectedSearchResult = null;
        }
    }

    private bool CanTakeOutCards() => this.SelectedDeckCard != null;

    [RelayCommand(CanExecute = nameof(CanTakeOutCards))]
    private void TakeCardFromDeck()
    {
        if (this.SelectedDeckCard != null && !this.CardsReplaced.Contains(this.SelectedDeckCard))
        {
            this.CardsReplaced.Add(this.SelectedDeckCard);
            this.SelectedDeckCard = null;
        }
    }

    private bool CanReturnCardToDeck() => this.SelectedReplaceCard != null;

    [RelayCommand(CanExecute = nameof(CanReturnCardToDeck))]
    private void ReturnCardToDeck()
    {
        if (this.SelectedReplaceCard != null)
        {
            this.CardsReplaced.Remove(this.SelectedReplaceCard);
            this.SelectedReplaceCard = null;
        }
    }

    private bool CanReturnLoanedCard() => this.SelectedLoanedCard != null;

    [RelayCommand(CanExecute = nameof(CanReturnLoanedCard))]
    private void ReturnLoanedCard()
    {
        if (this.SelectedLoanedCard != null)
        {
            this.LoanedOutCards.Remove(this.SelectedLoanedCard);
            this.SelectedLoanedCard = null;
        }
    }

    public LoanViewModel WithData(LoanModel loan)
    {
        this.Id = loan.Id;
        this.Name = loan.Name;
        this.ToDeckId = loan.ToDeckId;
        this.ToDeck = loan.ToDeckName;

        this.LoanedOutCards.Clear();
        foreach (var sku in loan.CardsOnLoan)
        {
            this.LoanedOutCards.Add(_cardSku().WithData(sku));
        }

        this.CardsReplaced.Clear();
        foreach (var sku in loan.ReplacedCardsInDeck)
        {
            this.CardsReplaced.Add(_cardSku().WithData(sku));
        }

        this.DeckCards.Clear();
        foreach (var sku in loan.DeckCards)
        {
            this.DeckCards.Add(_cardSku().WithData(sku));
        }

        return this;
    }
}
