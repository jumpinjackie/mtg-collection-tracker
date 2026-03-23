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
    private Action<IReadOnlyList<PlaytestCardViewModel>>? _reorderTopCardsAction;
    private Action<PlaytestCardViewModel>? _moveToHandAction;
    private Action<PlaytestCardViewModel>? _moveToGraveyardAction;
    private Action<PlaytestCardViewModel>? _moveToExileAction;
    private Action<PlaytestCardViewModel>? _moveToBottomAction;

    public ViewTopXViewModel()
        : this(WeakReferenceMessenger.Default) { }

    public ViewTopXViewModel(IMessenger messenger)
        : base(messenger)
    {
        _messenger = messenger;
    }

    public ObservableCollection<PlaytestCardViewModel> TopCards { get; } = new();

    [ObservableProperty]
    private PlaytestCardViewModel? _selectedCard;

    [ObservableProperty]
    private int _selectedCount;

    public bool HasSelection => SelectedCard is not null;

    public bool CanMoveSelectionUp => SelectedCard is not null && TopCards.IndexOf(SelectedCard) > 0;

    public bool CanMoveSelectionDown =>
        SelectedCard is not null
        && TopCards.IndexOf(SelectedCard) >= 0
        && TopCards.IndexOf(SelectedCard) < TopCards.Count - 1;

    public bool CanShuffleTop => TopCards.Count > 1;

    public ViewTopXViewModel Configure(
        IEnumerable<PlaytestCardViewModel> topCards,
        Action shuffleLibraryAction,
        Action<IReadOnlyList<PlaytestCardViewModel>> reorderTopCardsAction,
        Action<PlaytestCardViewModel> moveToHandAction,
        Action<PlaytestCardViewModel> moveToGraveyardAction,
        Action<PlaytestCardViewModel> moveToExileAction,
        Action<PlaytestCardViewModel> moveToBottomAction)
    {
        _shuffleLibraryAction = shuffleLibraryAction;
        _reorderTopCardsAction = reorderTopCardsAction;
        _moveToHandAction = moveToHandAction;
        _moveToGraveyardAction = moveToGraveyardAction;
        _moveToExileAction = moveToExileAction;
        _moveToBottomAction = moveToBottomAction;

        ReplaceTopCards(topCards.ToList());

        SelectedCard = TopCards.FirstOrDefault();
        UpdateSelectionState();
        return this;
    }

    partial void OnSelectedCardChanged(PlaytestCardViewModel? value)
    {
        UpdateSelectionState();
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
        ApplySingleCardAction(_moveToHandAction);
    }

    [RelayCommand(CanExecute = nameof(CanShuffleTop))]
    private void ShuffleTopAndClose()
    {
        if (TopCards.Count > 1)
        {
            var shuffledCards = TopCards.ToList();
            ShuffleCards(shuffledCards);
            ReplaceTopCards(shuffledCards, SelectedCard);
            SyncTopCardsToLibrary();
        }

        _messenger.Send(new CloseDialogMessage());
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void MoveToTopOfGraveyard()
    {
        ApplySingleCardAction(_moveToGraveyardAction);
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void MoveToExile()
    {
        ApplySingleCardAction(_moveToExileAction);
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void MoveToBottomOfLibrary()
    {
        ApplySingleCardAction(_moveToBottomAction);
    }

    [RelayCommand(CanExecute = nameof(CanMoveSelectionUp))]
    private void MoveSelectionUp()
    {
        MoveSelectionBy(-1);
    }

    [RelayCommand(CanExecute = nameof(CanMoveSelectionDown))]
    private void MoveSelectionDown()
    {
        MoveSelectionBy(1);
    }

    [RelayCommand]
    private void Close()
    {
        _messenger.Send(new CloseDialogMessage());
    }

    private void ApplySingleCardAction(Action<PlaytestCardViewModel>? action)
    {
        var card = SelectedCard;
        if (action is null || card is null)
        {
            return;
        }

        var previousIndex = TopCards.IndexOf(card);
        action(card);
        TopCards.Remove(card);

        if (TopCards.Count == 0)
        {
            SelectedCard = null;
            return;
        }

        var nextIndex = Math.Min(previousIndex, TopCards.Count - 1);
        SelectedCard = TopCards[nextIndex];
    }

    private void MoveSelectionBy(int offset)
    {
        var card = SelectedCard;
        if (card is null)
        {
            return;
        }

        var currentIndex = TopCards.IndexOf(card);
        var targetIndex = currentIndex + offset;
        if (currentIndex < 0 || targetIndex < 0 || targetIndex >= TopCards.Count)
        {
            return;
        }

        var reorderedCards = TopCards.ToList();
        reorderedCards.RemoveAt(currentIndex);
        reorderedCards.Insert(targetIndex, card);
        ReplaceTopCards(reorderedCards, card);
        SyncTopCardsToLibrary();
        UpdateSelectionState();
    }

    private void ReplaceTopCards(IReadOnlyList<PlaytestCardViewModel> cards, PlaytestCardViewModel? selectedCard = null)
    {
        TopCards.Clear();
        foreach (var card in cards)
        {
            TopCards.Add(card);
        }

        if (selectedCard is not null && TopCards.Contains(selectedCard))
        {
            SelectedCard = selectedCard;
        }
    }

    private void SyncTopCardsToLibrary()
    {
        _reorderTopCardsAction?.Invoke(TopCards.ToList());
    }

    private void UpdateSelectionState()
    {
        SelectedCount = SelectedCard is null ? 0 : 1;
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(CanMoveSelectionUp));
        OnPropertyChanged(nameof(CanMoveSelectionDown));
        OnPropertyChanged(nameof(CanShuffleTop));
        MoveToHandCommand.NotifyCanExecuteChanged();
        ShuffleTopAndCloseCommand.NotifyCanExecuteChanged();
        MoveToTopOfGraveyardCommand.NotifyCanExecuteChanged();
        MoveToExileCommand.NotifyCanExecuteChanged();
        MoveToBottomOfLibraryCommand.NotifyCanExecuteChanged();
        MoveSelectionUpCommand.NotifyCanExecuteChanged();
        MoveSelectionDownCommand.NotifyCanExecuteChanged();
    }

    private static void ShuffleCards<T>(IList<T> list)
    {
        for (var index = list.Count - 1; index > 0; index--)
        {
            var swapIndex = Random.Shared.Next(index + 1);
            (list[index], list[swapIndex]) = (list[swapIndex], list[index]);
        }
    }
}
