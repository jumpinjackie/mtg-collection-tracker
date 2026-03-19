using Avalonia.Controls;
using System;

namespace MtgCollectionTracker.Views;

public partial class LoadCardsView : UserControl
{
    public LoadCardsView()
    {
        InitializeComponent();
        this.AttachedToVisualTree += (_, _) => UpdateTextBoxMaxHeight();
        this.SizeChanged += (_, _) => UpdateTextBoxMaxHeight();
    }

    private void UpdateTextBoxMaxHeight()
    {
        var availableHeight = this.Bounds.Height
            - IntroTextBlock.Bounds.Height
            - BottomActionsPanel.Bounds.Height
            - 40;

        DecklistTextBox.MaxHeight = Math.Max(180, availableHeight);
    }
}
