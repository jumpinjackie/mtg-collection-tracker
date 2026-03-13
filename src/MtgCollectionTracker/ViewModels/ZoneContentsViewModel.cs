using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Services.Messaging;

namespace MtgCollectionTracker.ViewModels;

public partial class ZoneContentsViewModel : DialogContentViewModel
{
    private readonly IMessenger _messenger;
    private ObservableCollection<PlaytestCardViewModel>? _sourceCards;
    private Action<PlaytestCardViewModel, GameZone>? _moveCard;
    private Action? _shuffleLibrary;

    public ZoneContentsViewModel()
        : this(WeakReferenceMessenger.Default) { }

    public ZoneContentsViewModel(IMessenger messenger)
        : base(messenger)
    {
        _messenger = messenger;
        SelectedCards.CollectionChanged += OnSelectedCardsChanged;
    }

    [ObservableProperty]
    private string _filterText = string.Empty;

    [ObservableProperty]
    private PlaytestCardViewModel? _selectedCard;

    [ObservableProperty]
    private bool _showMoveToBattlefield;

    [ObservableProperty]
    private bool _showMoveToHand;

    [ObservableProperty]
    private bool _showMoveToGraveyard;

    [ObservableProperty]
    private bool _showMoveToExile;

    [ObservableProperty]
    private bool _showShuffleOption;

    [ObservableProperty]
    private bool _shuffleAfterClose;

    [ObservableProperty]
    private int _selectedCount;

    public bool CanMoveSelected => SelectedCards.Count > 0;

    public ObservableCollection<PlaytestCardViewModel> Cards { get; } = new();

    public ObservableCollection<PlaytestCardViewModel> SelectedCards { get; } = new();

    public ZoneContentsViewModel Configure(
        GameZone sourceZone,
        ObservableCollection<PlaytestCardViewModel> sourceCards,
        Action<PlaytestCardViewModel, GameZone> moveCard,
        Action? shuffleLibrary
    )
    {
        if (_sourceCards is not null)
        {
            _sourceCards.CollectionChanged -= OnSourceCardsChanged;
        }

        _sourceCards = sourceCards;
        _sourceCards.CollectionChanged += OnSourceCardsChanged;
        _moveCard = moveCard;
        _shuffleLibrary = shuffleLibrary;

        ShowMoveToBattlefield = true;
        ShowMoveToHand = true;
        ShowMoveToGraveyard = sourceZone != GameZone.Graveyard;
        ShowMoveToExile = sourceZone != GameZone.Exile;

        ShowShuffleOption = sourceZone == GameZone.Library;
        ShuffleAfterClose = ShowShuffleOption;

        UpdateFilteredCards();
        return this;
    }

    partial void OnFilterTextChanged(string value)
    {
        UpdateFilteredCards();
    }

    [RelayCommand(CanExecute = nameof(CanMoveSelected))]
    private void MoveToBattlefield()
    {
        MoveSelectedToZone(GameZone.Battlefield);
    }

    [RelayCommand(CanExecute = nameof(CanMoveSelected))]
    private void MoveToHand()
    {
        MoveSelectedToZone(GameZone.Hand);
    }

    [RelayCommand(CanExecute = nameof(CanMoveSelected))]
    private void MoveToGraveyard()
    {
        MoveSelectedToZone(GameZone.Graveyard);
    }

    [RelayCommand(CanExecute = nameof(CanMoveSelected))]
    private void MoveToExile()
    {
        MoveSelectedToZone(GameZone.Exile);
    }

    [RelayCommand]
    private void Close()
    {
        if (ShowShuffleOption && ShuffleAfterClose)
        {
            _shuffleLibrary?.Invoke();
        }

        _messenger.Send(new CloseDialogMessage());
    }

    private void MoveSelectedToZone(GameZone targetZone)
    {
        if (_moveCard is null)
        {
            return;
        }

        var selection = SelectedCards.ToList();
        foreach (var card in selection)
        {
            var destination = targetZone;
            if (targetZone == GameZone.Battlefield)
            {
                var isLandType =
                    card.IsLand
                    || (
                        !string.IsNullOrWhiteSpace(card.CardType)
                        && card.CardType.Contains("Land", StringComparison.OrdinalIgnoreCase)
                    );
                destination = isLandType ? GameZone.BattlefieldLands : GameZone.Battlefield;
            }

            _moveCard(card, destination);
        }

        // Close dialog after moving cards
        if (ShowShuffleOption && ShuffleAfterClose)
        {
            _shuffleLibrary?.Invoke();
        }

        _messenger.Send(new CloseDialogMessage());
    }

    private void OnSourceCardsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateFilteredCards();
    }

    private void OnSelectedCardsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateSelectionState();
    }

    private void UpdateFilteredCards()
    {
        Cards.Clear();

        if (_sourceCards is null)
        {
            return;
        }

        var filter = FilterText?.Trim() ?? string.Empty;
        foreach (var card in _sourceCards)
        {
            if (
                filter.Length == 0
                || (
                    !string.IsNullOrWhiteSpace(card.CardName)
                    && card.CardName.Contains(filter, StringComparison.OrdinalIgnoreCase)
                )
            )
            {
                Cards.Add(card);
            }
        }

        SelectedCards.Clear();
        SelectedCard = Cards.FirstOrDefault();
        UpdateSelectionState();
    }

    private void UpdateSelectionState()
    {
        SelectedCount = SelectedCards.Count;
        OnPropertyChanged(nameof(CanMoveSelected));
        MoveToBattlefieldCommand.NotifyCanExecuteChanged();
        MoveToHandCommand.NotifyCanExecuteChanged();
        MoveToGraveyardCommand.NotifyCanExecuteChanged();
        MoveToExileCommand.NotifyCanExecuteChanged();
    }
}
