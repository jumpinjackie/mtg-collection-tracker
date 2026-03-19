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
/// The commander can be moved to the Stack, Hand, Battlefield, Graveyard, or Library.
/// </summary>
public partial class CommandZoneViewModel : DialogContentViewModel
{
    private readonly IMessenger _messenger;
    private ObservableCollection<PlaytestCardViewModel>? _commandZoneCards;
    private Action<PlaytestCardViewModel, GameZone>? _moveCard;

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
        Action<PlaytestCardViewModel, GameZone> moveCard)
    {
        _commandZoneCards = commandZoneCards;
        _moveCard = moveCard;

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
        MoveSelectedTo(GameZone.Library);
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
