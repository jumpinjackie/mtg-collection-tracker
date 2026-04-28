using Avalonia;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.ViewModels;

namespace MtgCollectionTracker.Views;

public partial class PlaytestingView : UserControl
{
    private enum DragCandidateKind
    {
        None,
        Card,
        Library,
    }

    private static readonly IBrush DefaultDropZoneBrush = Brushes.Gray;
    private static readonly IBrush ActiveDropZoneBrush = Brushes.Gold;
    private static readonly IBrush LibraryDragPreviewBrush = new SolidColorBrush(Color.Parse("#7B4F2B"));
    private static readonly IBrush BattlefieldDragPreviewBrush = Brushes.White;
    private static readonly IBrush BattlefieldDragPreviewBorderBrush = Brushes.DimGray;

    // Card horizontal margin (left + right) used for hand reorder position calculation
    private const int HandCardTotalMargin = 6;
    private const int HandCardInterItemSpacing = 5;
    private const double LibraryPreviewWidth = 60;
    private const double LibraryPreviewHeight = 85;
    private const double DragPreviewPointerOffset = 12;
    private const double DragStartThreshold = 6;

    private PlaytestCardViewModel? _draggedCard;
    private Border? _activeDropZone;
    private bool _isDraggingLibraryTopCard;
    private DragCandidateKind _pendingDragCandidate = DragCandidateKind.None;
    private PlaytestCardViewModel? _pendingDragCard;
    private Point _pendingDragStart;

    // For hand card reordering
    private bool _isDraggingHandCard;
    private int _handDragSourceIndex = -1;
    private int _handDropTargetIndex = -1;

    public PlaytestingView()
    {
        InitializeComponent();
        AddHandler(InputElement.PointerMovedEvent, OnRootPointerMoved, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
        AddHandler(InputElement.PointerReleasedEvent, OnRootPointerReleased, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
    }

    private PlaytestingViewModel? GetActiveViewModel()
    {
        return DataContext is PlaytestingViewModel { IsInGame: true } viewModel
            ? viewModel
            : null;
    }

    private PlaytestGameStateViewModel? GetActiveGameState()
    {
        return GetActiveViewModel()?.GameState;
    }

    private bool TryGetActiveGameState([NotNullWhen(true)] out PlaytestGameStateViewModel? gameState)
    {
        gameState = GetActiveGameState();
        return gameState is not null;
    }

    private void OnLibraryDoubleTapped(object? sender, TappedEventArgs e)
    {
        var gameState = GetActiveGameState();
        if (gameState is not null)
        {
            gameState.DrawCardCommand.Execute(null);
        }
    }

    private void OnHandCardDoubleTapped(object? sender, TappedEventArgs e)
    {
        var gameState = GetActiveGameState();
        if (sender is Border border && border.DataContext is PlaytestCardViewModel card &&
            gameState is not null)
        {
            gameState.PlayCardFromHand(card);
            _draggedCard = null;
            _isDraggingHandCard = false;
            _handDragSourceIndex = -1;
            ClearDropZoneHighlight();
            e.Handled = true;
        }
    }

    private void OnBattlefieldCardDoubleTapped(object? sender, TappedEventArgs e)
    {
        var gameState = GetActiveGameState();
        if (sender is Border border && border.DataContext is PlaytestCardViewModel card &&
            gameState is not null)
        {
            gameState.ToggleTap(card);
        }
    }

    private void OnStackCardDoubleTapped(object? sender, TappedEventArgs e)
    {
        var gameState = GetActiveGameState();
        if (gameState is not null)
        {
            gameState.ResolveStack();
            _draggedCard = null;
            ClearDropZoneHighlight();
            e.Handled = true;
        }
    }

    private void OnShuffleLibraryClick(object? sender, RoutedEventArgs e)
    {
        var gameState = GetActiveGameState();
        if (gameState is not null)
        {
            gameState.ShuffleLibraryCommand.Execute(null);
        }
    }

    private async void OnDrawXClick(object? sender, RoutedEventArgs e)
    {
        var gameState = GetActiveGameState();
        if (gameState is not null)
        {
            var count = await PromptForPositiveIntAsync("Draw X Cards", 1);
            if (count is > 0)
            {
                gameState.DrawCards(count.Value);
            }
        }
    }

    private async void OnMillXClick(object? sender, RoutedEventArgs e)
    {
        var gameState = GetActiveGameState();
        if (gameState is not null)
        {
            var count = await PromptForPositiveIntAsync("Mill X Cards", 1);
            if (count is > 0)
            {
                gameState.MillCards(count.Value);
            }
        }
    }

    private async void OnExileTopXClick(object? sender, RoutedEventArgs e)
    {
        var gameState = GetActiveGameState();
        if (gameState is not null)
        {
            var count = await PromptForPositiveIntAsync("Exile Top X Cards", 1);
            if (count is > 0)
            {
                gameState.ExileTopCards(count.Value);
            }
        }
    }

    private void OnLibraryViewContentsClick(object? sender, RoutedEventArgs e)
    {
        var gameState = GetActiveGameState();
        if (gameState is not null)
        {
            gameState.OpenZoneContentsDialog(GameZone.Library);
        }
    }

    private async void OnLibraryViewTopXClick(object? sender, RoutedEventArgs e)
    {
        var gameState = GetActiveGameState();
        if (gameState is not null)
        {
            var count = await PromptForPositiveIntAsync("View Top X Cards", 5);
            if (count is > 0)
            {
                gameState.OpenViewTopXDialog(count.Value);
            }
        }
    }

    private void OnGraveyardViewContentsClick(object? sender, RoutedEventArgs e)
    {
        var gameState = GetActiveGameState();
        if (gameState is not null)
        {
            gameState.OpenZoneContentsDialog(GameZone.Graveyard);
        }
    }

    private void OnExileViewContentsClick(object? sender, RoutedEventArgs e)
    {
        var gameState = GetActiveGameState();
        if (gameState is not null)
        {
            gameState.OpenZoneContentsDialog(GameZone.Exile);
        }
    }

    private void OnViewSideboardClick(object? sender, RoutedEventArgs e)
    {
        var gameState = GetActiveGameState();
        if (gameState is not null)
        {
            gameState.OpenSideboardDialog();
        }
    }

    private void OnResolveStackClick(object? sender, RoutedEventArgs e)
    {
        var gameState = GetActiveGameState();
        if (gameState is not null)
        {
            gameState.ResolveStack();
        }
    }

    private void OnCounterStackClick(object? sender, RoutedEventArgs e)
    {
        var gameState = GetActiveGameState();
        if (gameState is not null)
        {
            gameState.CounterStack();
        }
    }

    private void OnStackReturnToHandClick(object? sender, RoutedEventArgs e)
    {
        var gameState = GetActiveGameState();
        if (gameState is not null)
        {
            gameState.ReturnStackToHand();
        }
    }

    private void OnStackExileClick(object? sender, RoutedEventArgs e)
    {
        var gameState = GetActiveGameState();
        if (gameState is not null)
        {
            gameState.ExileFromStack();
        }
    }

    private void OnHandSendToBottomClick(object? sender, RoutedEventArgs e)
    {
        var gameState = GetActiveGameState();
        if (TryGetCardFromSender(sender, out var card) && gameState is not null)
        {
            gameState.SendToBottomOfLibrary(card!);
        }
    }

    private void OnHandSendToTopClick(object? sender, RoutedEventArgs e)
    {
        var gameState = GetActiveGameState();
        if (TryGetCardFromSender(sender, out var card) && gameState is not null)
        {
            gameState.SendToTopOfLibrary(card!);
        }
    }

    private void OnHandDiscardClick(object? sender, RoutedEventArgs e)
    {
        var gameState = GetActiveGameState();
        if (TryGetCardFromSender(sender, out var card) && gameState is not null)
        {
            gameState.DiscardFromHand(card!);
        }
    }

    private void OnHandExileClick(object? sender, RoutedEventArgs e)
    {
        var gameState = GetActiveGameState();
        if (TryGetCardFromSender(sender, out var card) && gameState is not null)
        {
            gameState.ExileFromHand(card!);
        }
    }

    private void OnBattlefieldReturnToHandClick(object? sender, RoutedEventArgs e)
    {
        MoveBattlefieldCard(sender, GameZone.Hand);
    }

    private void OnBattlefieldSendToGraveyardClick(object? sender, RoutedEventArgs e)
    {
        MoveBattlefieldCard(sender, GameZone.Graveyard);
    }

    private void OnBattlefieldSendToExileClick(object? sender, RoutedEventArgs e)
    {
        MoveBattlefieldCard(sender, GameZone.Exile);
    }

    private void OnBattlefieldCreateTokenClick(object? sender, RoutedEventArgs e)
    {
        var gameState = GetActiveGameState();
        if (gameState is not null)
        {
            gameState.OpenCreateTokenDialog();
        }
    }

    private void OnCommandZoneClick(object? sender, RoutedEventArgs e)
    {
        var gameState = GetActiveGameState();
        if (gameState is not null)
        {
            gameState.OpenCommandZoneDialog();
        }
    }

    private void OnAddCounterClick(object? sender, RoutedEventArgs e)
    {
        var gameState = GetActiveGameState();
        if (TryGetCardFromSender(sender, out var card) &&
            gameState is not null &&
            card is not null)
        {
            gameState.OpenAddCounterDialog(card);
        }
    }

    private async void OnBattlefieldTransformClick(object? sender, RoutedEventArgs e)
    {
        if (!TryGetCardFromSender(sender, out var card) ||
            sender is not MenuItem menuItem ||
            card is null ||
            !card.IsDoubleFaced)
        {
            return;
        }

        var border = (menuItem.Parent as ContextMenu)?.PlacementTarget as Border;
        if (border is not null)
        {
            await AnimateCardFlipAsync(border);
        }

        var gameState = GetActiveGameState();
        if (gameState is not null)
            gameState.TransformCard(card);
        else
            card.Transform();
    }

    private void MoveBattlefieldCard(object? sender, GameZone destination)
    {
        var gameState = GetActiveGameState();
        if (TryGetCardFromSender(sender, out var card) && gameState is not null)
        {
            gameState.MoveCard(card!, destination);
        }
    }

    /// <summary>
    /// Handles the context menu opening for battlefield cards, adding:
    /// - Per-counter-type increment/decrement items
    /// - Multi-selection actions when multiple cards are selected
    /// </summary>
    private void OnBattlefieldCardContextMenuOpened(object? sender, RoutedEventArgs e)
    {
        if (sender is not ContextMenu menu)
            return;

        var gameState = GetActiveGameState();
        if (gameState is null)
            return;

        var card = TryGetCardFromContextMenu(menu);

        if (card is null)
            return;

        // Remove previously added dynamic items
        var dynamicItems = menu.Items
            .OfType<Control>()
            .Where(i => i.Tag?.ToString() == "dynamic")
            .ToList();
        foreach (var item in dynamicItems)
            menu.Items.Remove(item);

        // Add per-counter increment/decrement items if card has counters
        if (card.HasCounters)
        {
            var separator = new Separator { Tag = "dynamic" };
            menu.Items.Add(separator);

            foreach (var counter in card.Counters)
            {
                var counterName = counter.CounterName;
                var addItem = new MenuItem
                {
                    Header = $"+1 {counterName} counter",
                    Tag = "dynamic",
                };
                addItem.Click += (_, _) =>
                {
                    var currentGameState = GetActiveGameState();
                    if (currentGameState is null)
                    {
                        return;
                    }

                    currentGameState.AdjustCounter(card, counterName, 1);
                };
                menu.Items.Add(addItem);

                var removeItem = new MenuItem
                {
                    Header = $"-1 {counterName} counter",
                    Tag = "dynamic",
                };
                removeItem.Click += (_, _) =>
                {
                    var currentGameState = GetActiveGameState();
                    if (currentGameState is null)
                    {
                        return;
                    }

                    currentGameState.AdjustCounter(card, counterName, -1);
                };
                menu.Items.Add(removeItem);
            }
        }

        // Add multi-selection actions when more than one card is selected
        var selectedCards = gameState.SelectedBattlefieldCards;
        if (selectedCards.Count > 1 && selectedCards.Contains(card))
        {
            var multiSep = new Separator { Tag = "dynamic" };
            menu.Items.Add(multiSep);

            var count = selectedCards.Count;

            var tapSelected = new MenuItem
            {
                Header = $"Tap {count} selected",
                Tag = "dynamic",
            };
            tapSelected.Click += (_, _) =>
            {
                if (!TryGetActiveGameState(out var gameState))
                {
                    return;
                }

                gameState.TapSelectedCards();
            };
            menu.Items.Add(tapSelected);

            var untapSelected = new MenuItem
            {
                Header = $"Untap {count} selected",
                Tag = "dynamic",
            };
            untapSelected.Click += (_, _) =>
            {
                if (!TryGetActiveGameState(out var gameState))
                {
                    return;
                }

                gameState.UntapSelectedCards();
            };
            menu.Items.Add(untapSelected);

            var graveyardSelected = new MenuItem
            {
                Header = $"Send {count} selected to Graveyard",
                Tag = "dynamic",
            };
            graveyardSelected.Click += (_, _) =>
            {
                if (!TryGetActiveGameState(out var gameState))
                {
                    return;
                }

                gameState.MoveSelectedBattlefieldCardsTo(GameZone.Graveyard);
            };
            menu.Items.Add(graveyardSelected);

            var handSelected = new MenuItem
            {
                Header = $"Return {count} selected to Hand",
                Tag = "dynamic",
            };
            handSelected.Click += (_, _) =>
            {
                if (!TryGetActiveGameState(out var gameState))
                {
                    return;
                }

                gameState.MoveSelectedBattlefieldCardsTo(GameZone.Hand);
            };
            menu.Items.Add(handSelected);

            var exileSelected = new MenuItem
            {
                Header = $"Exile {count} selected",
                Tag = "dynamic",
            };
            exileSelected.Click += (_, _) =>
            {
                if (!TryGetActiveGameState(out var gameState))
                {
                    return;
                }

                gameState.MoveSelectedBattlefieldCardsTo(GameZone.Exile);
            };
            menu.Items.Add(exileSelected);
        }
    }

    private void OnCardPointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is Border border &&
            border.DataContext is PlaytestCardViewModel card &&
            TryGetActiveGameState(out var gameState))
        {
            gameState.SelectedCard = card;
        }
    }

    private void OnCardPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border border ||
            border.DataContext is not PlaytestCardViewModel card ||
            !TryGetActiveGameState(out var gameState))
        {
            return;
        }

        // Only handle selection/deselection on left-click; right-click opens the context menu
        // and must not alter the current selection so that multi-select menu items fire correctly.
        var isRightClick = e.GetCurrentPoint(null).Properties.IsRightButtonPressed;
        if (isRightClick)
        {
            return;
        }

        _pendingDragCandidate = DragCandidateKind.Card;
        _pendingDragCard = card;
        _pendingDragStart = e.GetPosition(RootGrid);

        // Handle battlefield card multi-selection via left Ctrl+Click
        if (IsBattlefieldCard(card))
        {
            var isCtrlHeld = e.KeyModifiers.HasFlag(KeyModifiers.Control) ||
                             e.KeyModifiers.HasFlag(KeyModifiers.Meta);
            var preserveExistingSelection = !isCtrlHeld &&
                                            card.IsSelected &&
                                            gameState.SelectedBattlefieldCards.Count > 1;

            if (!preserveExistingSelection)
            {
                gameState.ToggleBattlefieldCardSelection(card, isCtrlHeld);
            }
        }

        _isDraggingHandCard = false;
        _handDragSourceIndex = -1;
        _handDropTargetIndex = -1;
        ClearHandDropIndicator();
    }

    private void OnLibraryPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(null).Properties.IsLeftButtonPressed ||
            !TryGetActiveGameState(out var gameState) ||
            gameState.LibraryCount == 0)
        {
            return;
        }

        _pendingDragCandidate = DragCandidateKind.Library;
        _pendingDragCard = null;
        _pendingDragStart = e.GetPosition(RootGrid);
    }

    private static bool IsBattlefieldCard(PlaytestCardViewModel card)
    {
        return card.Zone == GameZone.Battlefield || card.Zone == GameZone.BattlefieldLands;
    }

    private void OnDropToBattlefield(object? sender, PointerReleasedEventArgs e) => MoveDraggedCardTo(GameZone.Battlefield);

    private void OnDropToBattlefieldLands(object? sender, PointerReleasedEventArgs e) => MoveDraggedCardTo(GameZone.BattlefieldLands);

    private void OnDropToHand(object? sender, PointerReleasedEventArgs e)
    {
        if (_isDraggingLibraryTopCard)
        {
            if (TryGetActiveGameState(out var drawGameState))
            {
                drawGameState.DrawCardCommand.Execute(null);
            }

            ResetDragState();
            e.Handled = true;
            return;
        }

        // If dragging a hand card within the hand zone, handle reordering
        var handItemsControl = HandItemsControl;
        if (_isDraggingHandCard && _draggedCard is not null && _draggedCard.Zone == GameZone.Hand &&
            TryGetActiveGameState(out var handGameState) && handItemsControl is not null)
        {
            var draggedHandCard = _draggedCard;
            var pointerPos = e.GetPosition(handItemsControl);
            var targetIndex = GetHandDropTargetIndex(pointerPos, handGameState);
            var sourceIndex = handGameState.Hand.IndexOf(draggedHandCard);

            if (targetIndex >= handGameState.Hand.Count)
            {
                targetIndex = handGameState.Hand.Count - 1;
            }

            if (targetIndex >= 0 && sourceIndex >= 0 && targetIndex != sourceIndex)
            {
                handGameState.Hand.Move(sourceIndex, targetIndex);
            }

            ResetDragState();
            e.Handled = true;
            return;
        }

        MoveDraggedCardTo(GameZone.Hand);
        e.Handled = true;
    }

    private int GetHandDropTargetIndex(Avalonia.Point pointerPos, PlaytestGameStateViewModel gameState)
    {
        var hand = gameState.Hand;
        if (hand.Count == 0)
            return 0;

        // Each card occupies card width + horizontal margins + item spacing.
        var stride = gameState.HandCardWidth + HandCardTotalMargin + HandCardInterItemSpacing;
        var hoveredIndex = Math.Max(0, Math.Min((int)(pointerPos.X / stride), hand.Count - 1));
        var localX = pointerPos.X - (hoveredIndex * stride);
        var isRightHalf = localX >= (stride / 2.0);

        return isRightHalf ? hoveredIndex + 1 : hoveredIndex;
    }

    private void OnDropToStack(object? sender, PointerReleasedEventArgs e) => MoveDraggedCardTo(GameZone.Stack);

    private void OnDropToGraveyard(object? sender, PointerReleasedEventArgs e) => MoveDraggedCardTo(GameZone.Graveyard);

    private void OnDropToExile(object? sender, PointerReleasedEventArgs e) => MoveDraggedCardTo(GameZone.Exile);

    private void HandleDropByZone(Border zone, PointerReleasedEventArgs e)
    {
        if (ReferenceEquals(zone, HandDropZone))
        {
            OnDropToHand(zone, e);
            return;
        }

        if (ReferenceEquals(zone, GraveyardZone))
        {
            MoveDraggedCardTo(GameZone.Graveyard);
            return;
        }

        if (ReferenceEquals(zone, ExileZone))
        {
            MoveDraggedCardTo(GameZone.Exile);
            return;
        }

        if (ReferenceEquals(zone, StackZone))
        {
            MoveDraggedCardTo(GameZone.Stack);
            return;
        }

        if (ReferenceEquals(zone, BattlefieldZone))
        {
            MoveDraggedCardTo(GameZone.Battlefield);
            return;
        }

        if (ReferenceEquals(zone, BattlefieldLandsZone))
        {
            MoveDraggedCardTo(GameZone.BattlefieldLands);
        }
    }

    private void MoveDraggedCardTo(GameZone destination)
    {
        if (_isDraggingLibraryTopCard)
        {
            if (destination == GameZone.Hand && TryGetActiveGameState(out var drawGameState))
            {
                drawGameState.DrawCardCommand.Execute(null);
            }

            ResetDragState();
            return;
        }

        if (_draggedCard is null)
        {
            return;
        }

        if (TryGetActiveGameState(out var gameState))
        {
            if (_draggedCard.Zone == destination)
            {
                ResetDragState();
                return;
            }

            var draggedCard = _draggedCard;
            if (ShouldMoveSelectedBattlefieldCards(draggedCard, destination, gameState))
            {
                var order = destination == GameZone.Graveyard
                    ? CardMoveOrder.Random
                    : CardMoveOrder.AsSelected;
                gameState.MoveSelectedBattlefieldCardsTo(destination, order);
            }
            else
            {
                gameState.MoveCard(draggedCard, destination);
            }

            gameState.SelectedCard = draggedCard;
        }

        ResetDragState();
    }

    private void OnDropZonePointerMoved(object? sender, PointerEventArgs e)
    {
        if (!HasActiveDrag() || sender is not Border zone)
        {
            return;
        }

        if (!CanDropOnZone(zone))
        {
            if (ReferenceEquals(_activeDropZone, zone))
            {
                ClearDropZoneHighlight();
            }

            if (ReferenceEquals(zone, HandDropZone))
            {
                ClearHandDropIndicator();
            }

            return;
        }

        var handItemsControl = HandItemsControl;
        var draggedCard = _draggedCard;
        if (ReferenceEquals(zone, HandDropZone) &&
            _isDraggingHandCard &&
            draggedCard is not null &&
            draggedCard.Zone == GameZone.Hand &&
            TryGetActiveGameState(out var gameState) &&
            handItemsControl is not null)
        {
            var pointerPos = e.GetPosition(handItemsControl);
            var targetIndex = GetHandDropTargetIndex(pointerPos, gameState);
            if (targetIndex != _handDropTargetIndex)
            {
                _handDropTargetIndex = targetIndex;
                ShowHandDropIndicator(targetIndex, gameState);
            }
        }
        else
        {
            ClearHandDropIndicator();
        }

        if (!ReferenceEquals(_activeDropZone, zone))
        {
            ClearDropZoneHighlight();
            _activeDropZone = zone;
            _activeDropZone.BorderBrush = ActiveDropZoneBrush;
        }
    }

    private void OnDropZonePointerExited(object? sender, PointerEventArgs e)
    {
        if (sender is Border zone && ReferenceEquals(_activeDropZone, zone))
        {
            ClearDropZoneHighlight();
        }

        if (sender is Border handZone && ReferenceEquals(handZone, HandDropZone))
        {
            ClearHandDropIndicator();
        }
    }

    private void OnRootPointerMoved(object? sender, PointerEventArgs e)
    {
        TryActivatePendingDrag(e);

        if (!HasActiveDrag())
        {
            return;
        }

        var pointerPosition = e.GetPosition(RootGrid);
        RefreshDragPreview(pointerPosition);

        var zone = ResolveDropZoneFromPoint(pointerPosition);
        if (zone is not null && CanDropOnZone(zone))
        {
            if (!ReferenceEquals(_activeDropZone, zone))
            {
                ClearDropZoneHighlight();
                _activeDropZone = zone;
                _activeDropZone.BorderBrush = ActiveDropZoneBrush;
            }

            return;
        }

        ClearDropZoneHighlight();
        ClearHandDropIndicator();
    }

    private void OnRootPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        ClearPendingDragCandidate();

        if (!HasActiveDrag())
        {
            return;
        }

        var zone = _activeDropZone;
        if (zone is null)
        {
            var pointerPosition = e.GetPosition(RootGrid);
            zone = ResolveDropZoneFromPoint(pointerPosition);
        }

        if (zone is not null && CanDropOnZone(zone))
        {
            HandleDropByZone(zone, e);
            e.Handled = true;
            return;
        }

        ResetDragState();
    }

    private Border? ResolveDropZoneFromPoint(Point point)
    {
        var current = this.GetVisualAt(point);

        while (current is not null)
        {
            if (current is Border border && IsKnownDropZone(border))
            {
                return border;
            }

            current = current.GetVisualParent();
        }

        return null;
    }

    private bool IsKnownDropZone(Border zone)
    {
        return ReferenceEquals(zone, HandDropZone) ||
               ReferenceEquals(zone, StackZone) ||
               ReferenceEquals(zone, GraveyardZone) ||
               ReferenceEquals(zone, ExileZone) ||
               ReferenceEquals(zone, BattlefieldZone) ||
               ReferenceEquals(zone, BattlefieldLandsZone);
    }

    private static bool TryGetCardFromSender(object? sender, out PlaytestCardViewModel? card)
    {
        card = null;

        if (sender is not MenuItem menuItem)
        {
            return false;
        }

        card = menuItem.DataContext as PlaytestCardViewModel;
        if (card is not null)
        {
            return true;
        }

        card = TryGetCardFromContextMenu(menuItem.Parent as ContextMenu);
        return card is not null;
    }

    private static PlaytestCardViewModel? TryGetCardFromContextMenu(ContextMenu? menu)
    {
        if (menu is null)
        {
            return null;
        }

        // Fast path when Avalonia provides PlacementTarget.
        if (menu.PlacementTarget?.DataContext is PlaytestCardViewModel cardFromPlacementTarget)
        {
            return cardFromPlacementTarget;
        }

        // Fallback: walk parent chain and stop at first matching DataContext.
        var current = menu.Parent as StyledElement;
        while (current is not null)
        {
            if (current.DataContext is PlaytestCardViewModel cardFromParent)
            {
                return cardFromParent;
            }

            current = current.Parent;
        }

        return null;
    }

    private void ClearDropZoneHighlight()
    {
        if (_activeDropZone is not null)
        {
            _activeDropZone.BorderBrush = DefaultDropZoneBrush;
            _activeDropZone = null;
        }
    }

    private static bool ShouldMoveSelectedBattlefieldCards(
        PlaytestCardViewModel draggedCard,
        GameZone destination,
        PlaytestGameStateViewModel gameState)
    {
        return IsBattlefieldCard(draggedCard) &&
               (destination == GameZone.Graveyard || destination == GameZone.Exile) &&
               gameState.SelectedBattlefieldCards.Contains(draggedCard);
    }

    private bool HasActiveDrag()
    {
        return _draggedCard is not null || _isDraggingLibraryTopCard;
    }

    private bool CanDropOnZone(Border zone)
    {
        if (_isDraggingLibraryTopCard)
        {
            return ReferenceEquals(zone, HandDropZone);
        }

        return _draggedCard is not null &&
               (ReferenceEquals(zone, HandDropZone) ||
                ReferenceEquals(zone, StackZone) ||
                ReferenceEquals(zone, GraveyardZone) ||
                ReferenceEquals(zone, ExileZone) ||
                ReferenceEquals(zone, BattlefieldZone) ||
                ReferenceEquals(zone, BattlefieldLandsZone));
    }

    private void RefreshDragPreview(Point pointerPosition)
    {
        if (_isDraggingLibraryTopCard)
        {
            ShowDragPreview(LibraryDragPreviewBrush, Brushes.Black, LibraryPreviewWidth, LibraryPreviewHeight, pointerPosition);
            return;
        }

        if (_draggedCard is not null && IsBattlefieldCard(_draggedCard) && TryGetActiveGameState(out var gameState))
        {
            ShowDragPreview(
                BattlefieldDragPreviewBrush,
                BattlefieldDragPreviewBorderBrush,
                gameState.CardWidth,
                gameState.CardHeight,
                pointerPosition);
            return;
        }

        HideDragPreview();
    }

    private void ShowDragPreview(IBrush background, IBrush borderBrush, double width, double height, Point pointerPosition)
    {
        if (DragPreview is null)
        {
            return;
        }

        DragPreview.Background = background;
        DragPreview.BorderBrush = borderBrush;
        DragPreview.Width = width;
        DragPreview.Height = height;
        Canvas.SetLeft(DragPreview, pointerPosition.X + DragPreviewPointerOffset);
        Canvas.SetTop(DragPreview, pointerPosition.Y + DragPreviewPointerOffset);
        DragPreview.IsVisible = true;
    }

    private void HideDragPreview()
    {
        if (DragPreview is not null)
        {
            DragPreview.IsVisible = false;
        }
    }

    private void ResetDragState()
    {
        _draggedCard = null;
        _isDraggingLibraryTopCard = false;
        ClearPendingDragCandidate();
        _isDraggingHandCard = false;
        _handDragSourceIndex = -1;
        _handDropTargetIndex = -1;
        ClearHandDropIndicator();
        HideDragPreview();
        ClearDropZoneHighlight();
    }

    private void ClearPendingDragCandidate()
    {
        _pendingDragCandidate = DragCandidateKind.None;
        _pendingDragCard = null;
    }

    private void TryActivatePendingDrag(PointerEventArgs e)
    {
        if (_pendingDragCandidate == DragCandidateKind.None || HasActiveDrag())
        {
            return;
        }

        var point = e.GetCurrentPoint(this);
        if (!point.Properties.IsLeftButtonPressed)
        {
            ClearPendingDragCandidate();
            return;
        }

        var pointerPosition = e.GetPosition(RootGrid);
        var dx = pointerPosition.X - _pendingDragStart.X;
        var dy = pointerPosition.Y - _pendingDragStart.Y;
        if ((dx * dx) + (dy * dy) < DragStartThreshold * DragStartThreshold)
        {
            return;
        }

        if (_pendingDragCandidate == DragCandidateKind.Library)
        {
            if (!TryGetActiveGameState(out var gameState) || gameState.LibraryCount == 0)
            {
                ClearPendingDragCandidate();
                return;
            }

            _isDraggingLibraryTopCard = true;
            _draggedCard = null;
            _isDraggingHandCard = false;
            _handDragSourceIndex = -1;
            _handDropTargetIndex = -1;
            ClearHandDropIndicator();
            ClearPendingDragCandidate();
            return;
        }

        if (_pendingDragCandidate == DragCandidateKind.Card && _pendingDragCard is not null)
        {
            var card = _pendingDragCard;
            _draggedCard = card;
            _isDraggingLibraryTopCard = false;

            if (card.Zone == GameZone.Hand && TryGetActiveGameState(out var gameState))
            {
                _isDraggingHandCard = true;
                _handDragSourceIndex = gameState.Hand.IndexOf(card);
                _handDropTargetIndex = _handDragSourceIndex;
                ShowHandDropIndicator(_handDropTargetIndex, gameState);
            }
            else
            {
                _isDraggingHandCard = false;
                _handDragSourceIndex = -1;
                _handDropTargetIndex = -1;
                ClearHandDropIndicator();
            }

            ClearPendingDragCandidate();
        }
    }

    private void ShowHandDropIndicator(int targetIndex, PlaytestGameStateViewModel gameState)
    {
        var handDropIndicator = HandDropIndicator;
        if (handDropIndicator is null)
        {
            return;
        }

        var stride = gameState.HandCardWidth + HandCardTotalMargin + HandCardInterItemSpacing;
        var safeIndex = Math.Max(0, Math.Min(targetIndex, gameState.Hand.Count));

        // Draw at left/right edge of the hovered card. End-of-hand draws at the right edge of the last card.
        var indicatorX = (safeIndex == gameState.Hand.Count)
            ? ((safeIndex - 1) * stride) + 3 + gameState.HandCardWidth
            : (safeIndex * stride) + 3;

        handDropIndicator.Margin = new Thickness(indicatorX, 3, 0, 0);
        handDropIndicator.IsVisible = true;
    }

    private void ClearHandDropIndicator()
    {
        if (HandDropIndicator is Border handDropIndicator)
        {
            handDropIndicator.IsVisible = false;
        }
    }

    private static async Task AnimateCardFlipAsync(Border border)
    {
        var image = border.GetVisualDescendants().OfType<Image>().FirstOrDefault();
        if (image is null)
        {
            return;
        }

        image.RenderTransformOrigin = RelativePoint.Center;
        var scale = image.RenderTransform as ScaleTransform ?? new ScaleTransform(1, 1);
        image.RenderTransform = scale;

        await AnimateScaleXAsync(scale, 1.0, 0.0, 120);
        await AnimateScaleXAsync(scale, 0.0, 1.0, 120);
    }

    private static async Task AnimateScaleXAsync(ScaleTransform transform, double from, double to, int durationMs)
    {
        const int steps = 8;
        for (var i = 0; i <= steps; i++)
        {
            var t = (double)i / steps;
            transform.ScaleX = from + ((to - from) * t);
            await Task.Delay(durationMs / steps);
        }
    }

    private async Task<int?> PromptForPositiveIntAsync(string title, int initialValue)
    {
        var tcs = new TaskCompletionSource<int?>();

        var input = new TextBox
        {
            Text = initialValue.ToString(),
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };

        var okButton = new Button { Content = "OK", MinWidth = 70 };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 70 };

        var window = new Window
        {
            Title = title,
            Width = 260,
            Height = 140,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Thickness(12),
                Spacing = 10,
                Children =
                {
                    new TextBlock { Text = "Enter a positive number:" },
                    input,
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Spacing = 8,
                        Children = { cancelButton, okButton },
                    },
                },
            },
        };

        void TrySubmit()
        {
            if (int.TryParse(input.Text, out var count) && count > 0)
            {
                tcs.TrySetResult(count);
                window.Close();
            }
        }

        okButton.Click += (_, _) => TrySubmit();

        input.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                TrySubmit();
                e.Handled = true;
            }
        };

        cancelButton.Click += (_, _) =>
        {
            tcs.TrySetResult(null);
            window.Close();
        };

        window.Closed += (_, _) => tcs.TrySetResult(null);

        window.Opened += (_, _) =>
        {
            // Auto-focus the text input and select all text
            input.Focus();
            input.SelectAll();
        };

        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner is not null)
        {
            await window.ShowDialog(owner);
        }
        else
        {
            window.Show();
        }

        return await tcs.Task;
    }
}
