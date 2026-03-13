using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Services.Messaging;

namespace MtgCollectionTracker.ViewModels;

public partial class MulliganSelectionViewModel : DialogContentViewModel
{
    private Action<IReadOnlyList<PlaytestCardViewModel>>? _confirmAction;
    private bool _isEnforcingLimit;

    public MulliganSelectionViewModel()
    {
        SelectedCards.CollectionChanged += OnSelectedCardsChanged;
    }

    [ObservableProperty]
    private int _requiredCount;

    [ObservableProperty]
    private int _selectedCount;

    [ObservableProperty]
    private PlaytestCardViewModel? _selectedCard;

    public string Instruction =>
        $"Select {RequiredCount} card(s) to put on the bottom of your library.";

    public bool CanConfirm => SelectedCount == RequiredCount && RequiredCount > 0;

    public ObservableCollection<PlaytestCardViewModel> Cards { get; } = new();

    public ObservableCollection<PlaytestCardViewModel> SelectedCards { get; } = new();

    public MulliganSelectionViewModel Configure(
        IEnumerable<PlaytestCardViewModel> cards,
        int requiredCount,
        Action<IReadOnlyList<PlaytestCardViewModel>> confirmAction
    )
    {
        Cards.Clear();
        foreach (var card in cards)
        {
            Cards.Add(card);
        }

        SelectedCards.Clear();
        RequiredCount = requiredCount;
        _confirmAction = confirmAction;

        OnPropertyChanged(nameof(Instruction));
        OnPropertyChanged(nameof(CanConfirm));
        return this;
    }

    [RelayCommand(CanExecute = nameof(CanConfirm))]
    private void Confirm()
    {
        if (_confirmAction is null)
        {
            return;
        }

        _confirmAction(SelectedCards.ToList());
        Messenger.Send(new CloseDialogMessage());
    }

    [RelayCommand]
    private void Cancel() => Messenger.Send(new CloseDialogMessage());

    private void OnSelectedCardsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_isEnforcingLimit)
        {
            return;
        }

        if (RequiredCount == 1 && e.NewItems is not null)
        {
            var newestItem = e.NewItems.OfType<PlaytestCardViewModel>().LastOrDefault();
            if (newestItem is not null && SelectedCards.Count > 1)
            {
                _isEnforcingLimit = true;
                try
                {
                    var itemsToRemove = SelectedCards
                        .Where(item => !ReferenceEquals(item, newestItem))
                        .ToList();

                    if (itemsToRemove.Count > 0)
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            foreach (var item in itemsToRemove)
                            {
                                SelectedCards.Remove(item);
                            }

                            UpdateSelectionState();
                        });
                    }
                }
                finally
                {
                    _isEnforcingLimit = false;
                }
            }
        }
        else if (RequiredCount > 0 && SelectedCards.Count > RequiredCount && e.NewItems is not null)
        {
            _isEnforcingLimit = true;
            try
            {
                var itemsToRemove = e
                    .NewItems.OfType<PlaytestCardViewModel>()
                    .Take(SelectedCards.Count - RequiredCount)
                    .ToList();

                if (itemsToRemove.Count > 0)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        foreach (var item in itemsToRemove)
                        {
                            SelectedCards.Remove(item);
                        }

                        UpdateSelectionState();
                    });
                }
            }
            finally
            {
                _isEnforcingLimit = false;
            }
        }

        UpdateSelectionState();
    }

    private void UpdateSelectionState()
    {
        SelectedCount = SelectedCards.Count;
        OnPropertyChanged(nameof(CanConfirm));
        ConfirmCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void SelectCardForDisplay(PlaytestCardViewModel? card)
    {
        SelectedCard = card;
        if (card != null)
        {
            if (SelectedCards.Contains(card))
            {
                SelectedCards.Remove(card);
            }
            else
            {
                if (SelectedCards.Count < RequiredCount)
                {
                    SelectedCards.Add(card);
                }
            }
        }
    }
}
