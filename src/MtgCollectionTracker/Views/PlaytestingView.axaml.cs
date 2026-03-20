using Avalonia;
using System;
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
    private static readonly IBrush DefaultDropZoneBrush = Brushes.Gray;
    private static readonly IBrush ActiveDropZoneBrush = Brushes.Gold;

    // Card horizontal margin (left + right) used for hand reorder position calculation
    private const int HandCardTotalMargin = 6;

    private PlaytestCardViewModel? _draggedCard;
    private Border? _activeDropZone;

    // For hand card reordering
    private bool _isDraggingHandCard;
    private int _handDragSourceIndex = -1;

    public PlaytestingView()
    {
        InitializeComponent();
    }

    private void OnLibraryDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is PlaytestingViewModel vm && vm.IsInGame)
        {
            vm.GameState.DrawCardCommand.Execute(null);
        }
    }

    private void OnHandCardDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border border && border.DataContext is PlaytestCardViewModel card &&
            DataContext is PlaytestingViewModel vm && vm.IsInGame)
        {
            vm.GameState.PlayCardFromHand(card);
            _draggedCard = null;
            _isDraggingHandCard = false;
            _handDragSourceIndex = -1;
            ClearDropZoneHighlight();
            e.Handled = true;
        }
    }

    private void OnBattlefieldCardDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border border && border.DataContext is PlaytestCardViewModel card)
        {
            card.IsTapped = !card.IsTapped;
        }
    }

    private void OnStackCardDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is PlaytestingViewModel vm && vm.IsInGame)
        {
            vm.GameState.ResolveStack();
            _draggedCard = null;
            ClearDropZoneHighlight();
            e.Handled = true;
        }
    }

    private void OnShuffleLibraryClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PlaytestingViewModel vm && vm.IsInGame)
        {
            vm.GameState.ShuffleLibraryCommand.Execute(null);
        }
    }

    private async void OnDrawXClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PlaytestingViewModel vm && vm.IsInGame)
        {
            var count = await PromptForPositiveIntAsync("Draw X Cards", 1);
            if (count is > 0)
            {
                vm.GameState.DrawCards(count.Value);
            }
        }
    }

    private async void OnMillXClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PlaytestingViewModel vm && vm.IsInGame)
        {
            var count = await PromptForPositiveIntAsync("Mill X Cards", 1);
            if (count is > 0)
            {
                vm.GameState.MillCards(count.Value);
            }
        }
    }

    private async void OnExileTopXClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PlaytestingViewModel vm && vm.IsInGame)
        {
            var count = await PromptForPositiveIntAsync("Exile Top X Cards", 1);
            if (count is > 0)
            {
                vm.GameState.ExileTopCards(count.Value);
            }
        }
    }

    private void OnLibraryViewContentsClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PlaytestingViewModel vm && vm.IsInGame)
        {
            vm.GameState.OpenZoneContentsDialog(GameZone.Library);
        }
    }

    private async void OnLibraryViewTopXClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PlaytestingViewModel vm && vm.IsInGame)
        {
            var count = await PromptForPositiveIntAsync("View Top X Cards", 5);
            if (count is > 0)
            {
                vm.GameState.OpenViewTopXDialog(count.Value);
            }
        }
    }

    private void OnGraveyardViewContentsClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PlaytestingViewModel vm && vm.IsInGame)
        {
            vm.GameState.OpenZoneContentsDialog(GameZone.Graveyard);
        }
    }

    private void OnExileViewContentsClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PlaytestingViewModel vm && vm.IsInGame)
        {
            vm.GameState.OpenZoneContentsDialog(GameZone.Exile);
        }
    }

    private void OnViewSideboardClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PlaytestingViewModel vm && vm.IsInGame)
        {
            vm.GameState.OpenSideboardDialog();
        }
    }

    private void OnResolveStackClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PlaytestingViewModel vm && vm.IsInGame)
        {
            vm.GameState.ResolveStack();
        }
    }

    private void OnCounterStackClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PlaytestingViewModel vm && vm.IsInGame)
        {
            vm.GameState.CounterStack();
        }
    }

    private void OnStackReturnToHandClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PlaytestingViewModel vm && vm.IsInGame)
        {
            vm.GameState.ReturnStackToHand();
        }
    }

    private void OnStackExileClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PlaytestingViewModel vm && vm.IsInGame)
        {
            vm.GameState.ExileFromStack();
        }
    }

    private void OnHandSendToBottomClick(object? sender, RoutedEventArgs e)
    {
        if (TryGetCardFromSender(sender, out var card) &&
            DataContext is PlaytestingViewModel vm &&
            vm.IsInGame)
        {
            vm.GameState.SendToBottomOfLibrary(card!);
        }
    }

    private void OnHandSendToTopClick(object? sender, RoutedEventArgs e)
    {
        if (TryGetCardFromSender(sender, out var card) &&
            DataContext is PlaytestingViewModel vm &&
            vm.IsInGame)
        {
            vm.GameState.SendToTopOfLibrary(card!);
        }
    }

    private void OnHandDiscardClick(object? sender, RoutedEventArgs e)
    {
        if (TryGetCardFromSender(sender, out var card) &&
            DataContext is PlaytestingViewModel vm &&
            vm.IsInGame)
        {
            vm.GameState.DiscardFromHand(card!);
        }
    }

    private void OnHandExileClick(object? sender, RoutedEventArgs e)
    {
        if (TryGetCardFromSender(sender, out var card) &&
            DataContext is PlaytestingViewModel vm &&
            vm.IsInGame)
        {
            vm.GameState.ExileFromHand(card!);
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
        if (DataContext is PlaytestingViewModel vm && vm.IsInGame)
        {
            vm.GameState.OpenCreateTokenDialog();
        }
    }

    private void OnCommandZoneClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PlaytestingViewModel vm && vm.IsInGame)
        {
            vm.GameState.OpenCommandZoneDialog();
        }
    }

    private void OnAddCounterClick(object? sender, RoutedEventArgs e)
    {
        if (TryGetCardFromSender(sender, out var card) &&
            DataContext is PlaytestingViewModel vm &&
            vm.IsInGame &&
            card is not null)
        {
            vm.GameState.OpenAddCounterDialog(card);
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

        card.Transform();
    }

    private void MoveBattlefieldCard(object? sender, GameZone destination)
    {
        if (TryGetCardFromSender(sender, out var card) &&
            DataContext is PlaytestingViewModel vm &&
            vm.IsInGame)
        {
            vm.GameState.MoveCard(card!, destination);
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

        if (DataContext is not PlaytestingViewModel vm || !vm.IsInGame)
            return;

        var card = menu.PlacementTarget?.DataContext as PlaytestCardViewModel;
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
                    if (DataContext is PlaytestingViewModel v && v.IsInGame)
                        v.GameState.AdjustCounter(card, counterName, 1);
                };
                menu.Items.Add(addItem);

                var removeItem = new MenuItem
                {
                    Header = $"-1 {counterName} counter",
                    Tag = "dynamic",
                };
                removeItem.Click += (_, _) =>
                {
                    if (DataContext is PlaytestingViewModel v && v.IsInGame)
                        v.GameState.AdjustCounter(card, counterName, -1);
                };
                menu.Items.Add(removeItem);
            }
        }

        // Add multi-selection actions when more than one card is selected
        var selectedCards = vm.GameState.SelectedBattlefieldCards;
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
                if (DataContext is PlaytestingViewModel v && v.IsInGame)
                    v.GameState.TapSelectedCards();
            };
            menu.Items.Add(tapSelected);

            var untapSelected = new MenuItem
            {
                Header = $"Untap {count} selected",
                Tag = "dynamic",
            };
            untapSelected.Click += (_, _) =>
            {
                if (DataContext is PlaytestingViewModel v && v.IsInGame)
                    v.GameState.UntapSelectedCards();
            };
            menu.Items.Add(untapSelected);

            var graveyardSelected = new MenuItem
            {
                Header = $"Send {count} selected to Graveyard",
                Tag = "dynamic",
            };
            graveyardSelected.Click += (_, _) =>
            {
                if (DataContext is PlaytestingViewModel v && v.IsInGame)
                    v.GameState.MoveSelectedBattlefieldCardsTo(GameZone.Graveyard);
            };
            menu.Items.Add(graveyardSelected);

            var handSelected = new MenuItem
            {
                Header = $"Return {count} selected to Hand",
                Tag = "dynamic",
            };
            handSelected.Click += (_, _) =>
            {
                if (DataContext is PlaytestingViewModel v && v.IsInGame)
                    v.GameState.MoveSelectedBattlefieldCardsTo(GameZone.Hand);
            };
            menu.Items.Add(handSelected);

            var exileSelected = new MenuItem
            {
                Header = $"Exile {count} selected",
                Tag = "dynamic",
            };
            exileSelected.Click += (_, _) =>
            {
                if (DataContext is PlaytestingViewModel v && v.IsInGame)
                    v.GameState.MoveSelectedBattlefieldCardsTo(GameZone.Exile);
            };
            menu.Items.Add(exileSelected);
        }
    }

    private void OnCardPointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is Border border &&
            border.DataContext is PlaytestCardViewModel card &&
            DataContext is PlaytestingViewModel vm &&
            vm.IsInGame)
        {
            vm.GameState.SelectedCard = card;
        }
    }

    private void OnCardPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border border ||
            border.DataContext is not PlaytestCardViewModel card ||
            DataContext is not PlaytestingViewModel vm ||
            !vm.IsInGame)
        {
            return;
        }

        _draggedCard = card;

        // Handle battlefield card multi-selection via Ctrl+Click
        if (IsBattlefieldCard(card))
        {
            var isCtrlHeld = e.KeyModifiers.HasFlag(KeyModifiers.Control) ||
                             e.KeyModifiers.HasFlag(KeyModifiers.Meta);
            vm.GameState.ToggleBattlefieldCardSelection(card, isCtrlHeld);
        }

        // Track hand card drag for reordering
        if (card.Zone == GameZone.Hand)
        {
            _isDraggingHandCard = true;
            _handDragSourceIndex = vm.GameState.Hand.IndexOf(card);
        }
        else
        {
            _isDraggingHandCard = false;
            _handDragSourceIndex = -1;
        }
    }

    private static bool IsBattlefieldCard(PlaytestCardViewModel card)
    {
        return card.Zone == GameZone.Battlefield || card.Zone == GameZone.BattlefieldLands;
    }

    private void OnDropToBattlefield(object? sender, PointerReleasedEventArgs e) => MoveDraggedCardTo(GameZone.Battlefield);

    private void OnDropToBattlefieldLands(object? sender, PointerReleasedEventArgs e) => MoveDraggedCardTo(GameZone.BattlefieldLands);

    private void OnDropToHand(object? sender, PointerReleasedEventArgs e)
    {
        // If dragging a hand card within the hand zone, handle reordering
        if (_isDraggingHandCard && _draggedCard is not null && _draggedCard.Zone == GameZone.Hand &&
            DataContext is PlaytestingViewModel vm && vm.IsInGame)
        {
            var pointerPos = e.GetPosition(HandItemsControl);
            var targetIndex = GetHandDropIndex(pointerPos, vm);
            var sourceIndex = vm.GameState.Hand.IndexOf(_draggedCard);

            if (targetIndex >= 0 && sourceIndex >= 0 && targetIndex != sourceIndex)
            {
                vm.GameState.Hand.Move(sourceIndex, targetIndex);
            }

            _draggedCard = null;
            _isDraggingHandCard = false;
            _handDragSourceIndex = -1;
            ClearDropZoneHighlight();
            return;
        }

        MoveDraggedCardTo(GameZone.Hand);
    }

    private int GetHandDropIndex(Avalonia.Point pointerPos, PlaytestingViewModel vm)
    {
        var hand = vm.GameState.Hand;
        if (hand.Count == 0)
            return 0;

        // Each card is approximately CardWidth + margin (HandCardTotalMargin = 3+3=6) wide
        var cardWidth = vm.GameState.CardWidth + HandCardTotalMargin;
        var index = (int)(pointerPos.X / cardWidth);
        return Math.Max(0, Math.Min(index, hand.Count - 1));
    }

    private void OnDropToStack(object? sender, PointerReleasedEventArgs e) => MoveDraggedCardTo(GameZone.Stack);

    private void OnDropToGraveyard(object? sender, PointerReleasedEventArgs e) => MoveDraggedCardTo(GameZone.Graveyard);

    private void OnDropToExile(object? sender, PointerReleasedEventArgs e) => MoveDraggedCardTo(GameZone.Exile);

    private void MoveDraggedCardTo(GameZone destination)
    {
        if (_draggedCard is null)
        {
            return;
        }

        if (DataContext is PlaytestingViewModel vm && vm.IsInGame)
        {
            if (_draggedCard.Zone == destination)
            {
                _draggedCard = null;
                _isDraggingHandCard = false;
                _handDragSourceIndex = -1;
                ClearDropZoneHighlight();
                return;
            }
            vm.GameState.MoveCard(_draggedCard, destination);
            vm.GameState.SelectedCard = _draggedCard;
        }

        _draggedCard = null;
        _isDraggingHandCard = false;
        _handDragSourceIndex = -1;
        ClearDropZoneHighlight();
    }

    private void OnDropZonePointerMoved(object? sender, PointerEventArgs e)
    {
        if (_draggedCard is null || sender is not Border zone)
        {
            return;
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

        card = (menuItem.Parent as ContextMenu)?.PlacementTarget?.DataContext as PlaytestCardViewModel;
        return card is not null;
    }

    private void ClearDropZoneHighlight()
    {
        if (_activeDropZone is not null)
        {
            _activeDropZone.BorderBrush = DefaultDropZoneBrush;
            _activeDropZone = null;
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
