using Avalonia;
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

    private PlaytestCardViewModel? _draggedCard;
    private Border? _activeDropZone;

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
        if (sender is Border border &&
            border.DataContext is PlaytestCardViewModel card &&
            DataContext is PlaytestingViewModel vm &&
            vm.IsInGame)
        {
            _draggedCard = card;
        }
    }

    private void OnDropToBattlefield(object? sender, PointerReleasedEventArgs e) => MoveDraggedCardTo(GameZone.Battlefield);

    private void OnDropToBattlefieldLands(object? sender, PointerReleasedEventArgs e) => MoveDraggedCardTo(GameZone.BattlefieldLands);

    private void OnDropToHand(object? sender, PointerReleasedEventArgs e) => MoveDraggedCardTo(GameZone.Hand);

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
                ClearDropZoneHighlight();
                return;
            }
            vm.GameState.MoveCard(_draggedCard, destination);
            vm.GameState.SelectedCard = _draggedCard;
        }

        _draggedCard = null;
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
