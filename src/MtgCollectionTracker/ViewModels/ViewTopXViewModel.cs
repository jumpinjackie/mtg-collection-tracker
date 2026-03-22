using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Services.Messaging;

namespace MtgCollectionTracker.ViewModels;

/// <summary>
/// ViewModel for the "View Top X Cards" dialog which shows the top X cards of the library
/// and allows various actions on the selected cards.
/// </summary>
public partial class ViewTopXViewModel : DialogContentViewModel
{
    private readonly IMessenger _messenger;
    private Action? _shuffleLibraryAction;
    private Action<IEnumerable<PlaytestCardViewModel>, CardMoveOrder>? _moveToHandAction;
    private Action<IEnumerable<PlaytestCardViewModel>, CardMoveOrder>? _moveToGraveyardAction;
    private Action<IEnumerable<PlaytestCardViewModel>, CardMoveOrder>? _moveToExileAction;
    private Action<IEnumerable<PlaytestCardViewModel>, CardMoveOrder>? _moveToBottomAction;
    private Action<IEnumerable<PlaytestCardViewModel>, CardMoveOrder>? _moveToTopAction;

    public ViewTopXViewModel()
        : this(WeakReferenceMessenger.Default) { }

    public ViewTopXViewModel(IMessenger messenger)
        : base(messenger)
    {
        _messenger = messenger;
        SelectedCards.CollectionChanged += (_, _) => UpdateSelectionState();
    }

    public ObservableCollection<PlaytestCardViewModel> TopCards { get; } = new();

    public ObservableCollection<PlaytestCardViewModel> SelectedCards { get; } = new();

    [ObservableProperty]
    private PlaytestCardViewModel? _selectedCard;

    [ObservableProperty]
    private int _selectedCount;

    public bool HasSelection => SelectedCards.Count > 0;

    public ViewTopXViewModel Configure(
        IEnumerable<PlaytestCardViewModel> topCards,
        Action shuffleLibraryAction,
        Action<IEnumerable<PlaytestCardViewModel>, CardMoveOrder> moveToHandAction,
        Action<IEnumerable<PlaytestCardViewModel>, CardMoveOrder> moveToGraveyardAction,
        Action<IEnumerable<PlaytestCardViewModel>, CardMoveOrder> moveToExileAction,
        Action<IEnumerable<PlaytestCardViewModel>, CardMoveOrder> moveToBottomAction,
        Action<IEnumerable<PlaytestCardViewModel>, CardMoveOrder> moveToTopAction)
    {
        _shuffleLibraryAction = shuffleLibraryAction;
        _moveToHandAction = moveToHandAction;
        _moveToGraveyardAction = moveToGraveyardAction;
        _moveToExileAction = moveToExileAction;
        _moveToBottomAction = moveToBottomAction;
        _moveToTopAction = moveToTopAction;

        TopCards.Clear();
        foreach (var card in topCards)
            TopCards.Add(card);

        SelectedCard = TopCards.FirstOrDefault();
        return this;
    }

    [RelayCommand]
    private void ShuffleAndClose()
    {
        _shuffleLibraryAction?.Invoke();
        _messenger.Send(new CloseDialogMessage());
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void MoveToHand()
    {
        _moveToHandAction?.Invoke(SelectedCards.ToList(), CardMoveOrder.AsSelected);
        _messenger.Send(new CloseDialogMessage());
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void MoveToGraveyardRandom()
    {
        _moveToGraveyardAction?.Invoke(SelectedCards.ToList(), CardMoveOrder.Random);
        _messenger.Send(new CloseDialogMessage());
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void MoveToGraveyardAsSelected()
    {
        _moveToGraveyardAction?.Invoke(SelectedCards.ToList(), CardMoveOrder.AsSelected);
        _messenger.Send(new CloseDialogMessage());
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void MoveToExile()
    {
        _moveToExileAction?.Invoke(SelectedCards.ToList(), CardMoveOrder.AsSelected);
        _messenger.Send(new CloseDialogMessage());
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void MoveToBottomRandom()
    {
        _moveToBottomAction?.Invoke(SelectedCards.ToList(), CardMoveOrder.Random);
        _messenger.Send(new CloseDialogMessage());
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void MoveToBottomAsSelected()
    {
        _moveToBottomAction?.Invoke(SelectedCards.ToList(), CardMoveOrder.AsSelected);
        _messenger.Send(new CloseDialogMessage());
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void MoveToTopAsSelected()
    {
        _moveToTopAction?.Invoke(SelectedCards.ToList(), CardMoveOrder.AsSelected);
        _messenger.Send(new CloseDialogMessage());
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void MoveToTopRandom()
    {
        _moveToTopAction?.Invoke(SelectedCards.ToList(), CardMoveOrder.Random);
        _messenger.Send(new CloseDialogMessage());
    }

    [RelayCommand]
    private void Close()
    {
        _messenger.Send(new CloseDialogMessage());
    }

    private void UpdateSelectionState()
    {
        SelectedCount = SelectedCards.Count;
        OnPropertyChanged(nameof(HasSelection));
        MoveToHandCommand.NotifyCanExecuteChanged();
        MoveToGraveyardRandomCommand.NotifyCanExecuteChanged();
        MoveToGraveyardAsSelectedCommand.NotifyCanExecuteChanged();
        MoveToExileCommand.NotifyCanExecuteChanged();
        MoveToBottomRandomCommand.NotifyCanExecuteChanged();
        MoveToBottomAsSelectedCommand.NotifyCanExecuteChanged();
        MoveToTopAsSelectedCommand.NotifyCanExecuteChanged();
        MoveToTopRandomCommand.NotifyCanExecuteChanged();
    }
}
