using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Services.Messaging;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace MtgCollectionTracker.ViewModels;

/// <summary>
/// ViewModel for displaying and interacting with the Command Zone in Commander games.
/// The commander can be moved to the Stack, Hand, Battlefield, Graveyard, or Library (top/bottom/random+shuffle).
/// </summary>
public partial class CommandZoneViewModel : DialogContentViewModel
{
    private readonly IMessenger _messenger;
    private ObservableCollection<PlaytestCardViewModel>? _commandZoneCards;
    private Action<PlaytestCardViewModel, GameZone>? _moveCard;
    private Action<PlaytestCardViewModel>? _moveToTopOfLibrary;
    private Action<PlaytestCardViewModel>? _moveToBottomOfLibrary;
    private Action<PlaytestCardViewModel>? _moveToLibraryAndShuffle;

    public CommandZoneViewModel()
        : this(WeakReferenceMessenger.Default) { }

    public CommandZoneViewModel(IMessenger messenger)
        : base(messenger)
    {
        _messenger = messenger;
    }

    public ObservableCollection<PlaytestCardViewModel> Cards { get; } = new();

    [ObservableProperty]
    private PlaytestCardViewModel? _selectedCard;

    private bool CanMove() => SelectedCard != null;

    public CommandZoneViewModel Configure(
        ObservableCollection<PlaytestCardViewModel> commandZoneCards,
        Action<PlaytestCardViewModel, GameZone> moveCard,
        Action<PlaytestCardViewModel>? moveToTopOfLibrary = null,
        Action<PlaytestCardViewModel>? moveToBottomOfLibrary = null,
        Action<PlaytestCardViewModel>? moveToLibraryAndShuffle = null)
    {
        _commandZoneCards = commandZoneCards;
        _moveCard = moveCard;
        _moveToTopOfLibrary = moveToTopOfLibrary;
        _moveToBottomOfLibrary = moveToBottomOfLibrary;
        _moveToLibraryAndShuffle = moveToLibraryAndShuffle;

        Cards.Clear();
        foreach (var card in commandZoneCards)
        {
            Cards.Add(card);
        }

        SelectedCard = Cards.FirstOrDefault();
        return this;
    }

    [RelayCommand(CanExecute = nameof(CanMove))]
    private void MoveToStack()
    {
        MoveSelectedTo(GameZone.Stack);
    }

    [RelayCommand(CanExecute = nameof(CanMove))]
    private void MoveToHand()
    {
        MoveSelectedTo(GameZone.Hand);
    }

    [RelayCommand(CanExecute = nameof(CanMove))]
    private void MoveToBattlefield()
    {
        MoveSelectedTo(GameZone.Battlefield);
    }

    [RelayCommand(CanExecute = nameof(CanMove))]
    private void MoveToGraveyard()
    {
        MoveSelectedTo(GameZone.Graveyard);
    }

    [RelayCommand(CanExecute = nameof(CanMove))]
    private void MoveToTopOfLibrary()
    {
        if (_moveToTopOfLibrary is null || SelectedCard is null)
            return;

        _moveToTopOfLibrary(SelectedCard);
        _messenger.Send(new CloseDialogMessage());
    }

    [RelayCommand(CanExecute = nameof(CanMove))]
    private void MoveToBottomOfLibrary()
    {
        if (_moveToBottomOfLibrary is null || SelectedCard is null)
            return;

        _moveToBottomOfLibrary(SelectedCard);
        _messenger.Send(new CloseDialogMessage());
    }

    [RelayCommand(CanExecute = nameof(CanMove))]
    private void MoveToLibraryAndShuffle()
    {
        if (_moveToLibraryAndShuffle is null || SelectedCard is null)
            return;

        _moveToLibraryAndShuffle(SelectedCard);
        _messenger.Send(new CloseDialogMessage());
    }

    [RelayCommand]
    private void Close()
    {
        _messenger.Send(new CloseDialogMessage());
    }

    private void MoveSelectedTo(GameZone targetZone)
    {
        if (_moveCard is null || SelectedCard is null)
            return;

        _moveCard(SelectedCard, targetZone);
        _messenger.Send(new CloseDialogMessage());
    }
}
