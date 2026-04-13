using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;

namespace MtgCollectionTracker.Views;

public partial class AddCardsView : UserControl
{
    private bool _isGridEditing;

    public AddCardsView()
    {
        InitializeComponent();
        this.AttachedToVisualTree += (_, _) => UpdateGridMaxHeight();
        this.SizeChanged += (_, _) => UpdateGridMaxHeight();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        UpdateGridMaxHeight();
        AddCardsDataGrid.PreparingCellForEdit += OnPreparingCellForEdit;
        AddCardsDataGrid.CellEditEnded += OnCellEditEnded;
        AddCardsDataGrid.AddHandler(InputElement.TextInputEvent, OnDataGridTextInput, RoutingStrategies.Bubble);
        AddCardsDataGrid.AddHandler(InputElement.KeyDownEvent, OnDataGridKeyDown, RoutingStrategies.Tunnel);
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        AddCardsDataGrid.PreparingCellForEdit -= OnPreparingCellForEdit;
        AddCardsDataGrid.CellEditEnded -= OnCellEditEnded;
    }

    private void OnPreparingCellForEdit(object? sender, DataGridPreparingCellForEditEventArgs e)
        => _isGridEditing = true;

    private void OnCellEditEnded(object? sender, DataGridCellEditEndedEventArgs e)
        => _isGridEditing = false;

    private void UpdateGridMaxHeight()
    {
        var availableHeight = this.Bounds.Height
            - IntroTextBlock.Bounds.Height
            - BottomActionsGrid.Bounds.Height
            - 24;

        AddCardsDataGrid.MaxHeight = Math.Max(180, availableHeight);
    }

    private void OnDataGridTextInput(object? sender, TextInputEventArgs e)
    {
        if (_isGridEditing || string.IsNullOrEmpty(e.Text))
        {
            return;
        }

        var grid = (DataGrid)sender!;
        if (!grid.BeginEdit())
        {
            return;
        }

        e.Handled = true;

        var pendingText = e.Text;
        Dispatcher.UIThread.Post(() =>
        {
            var topLevel = TopLevel.GetTopLevel(grid);
            if (topLevel?.FocusManager?.GetFocusedElement() is TextBox textBox)
            {
                textBox.Text = pendingText;
                textBox.CaretIndex = pendingText.Length;
            }
        }, DispatcherPriority.Background);
    }

    private void OnDataGridKeyDown(object? sender, KeyEventArgs e)
    {
        if (_isGridEditing || !IsPasteShortcut(e))
        {
            return;
        }

        var grid = (DataGrid)sender!;
        if (!grid.BeginEdit())
        {
            return;
        }

        e.Handled = true;
        Dispatcher.UIThread.Post(() => InjectClipboardText(grid), DispatcherPriority.Background);
    }

    private static bool IsPasteShortcut(KeyEventArgs e)
    {
        if (e.Key == Key.Insert)
        {
            return e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        }

        if (e.Key == Key.V)
        {
            return e.KeyModifiers.HasFlag(KeyModifiers.Control)
                || e.KeyModifiers.HasFlag(KeyModifiers.Meta);
        }

        return false;
    }

    private async void InjectClipboardText(DataGrid grid)
    {
        var topLevel = TopLevel.GetTopLevel(grid);
        if (topLevel?.FocusManager?.GetFocusedElement() is TextBox textBox
            && topLevel.Clipboard is { } clipboard)
        {
            var text = await clipboard.TryGetTextAsync();
            if (!string.IsNullOrEmpty(text))
            {
                textBox.Text = text;
                textBox.CaretIndex = text.Length;
            }
        }
    }
}