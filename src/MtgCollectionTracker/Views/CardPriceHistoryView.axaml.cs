using Avalonia.Controls;
using MtgCollectionTracker.ViewModels;
using System;

namespace MtgCollectionTracker.Views;

public partial class CardPriceHistoryView : UserControl
{
    private const double DateLabelReserveHeight = 32.0;

    public CardPriceHistoryView()
    {
        InitializeComponent();
    }

    private void ChartScrollViewer_OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext is not CardPriceHistoryViewModel vm)
        {
            return;
        }

        vm.ChartPlotHeight = Math.Max(1.0, e.NewSize.Height - DateLabelReserveHeight);
    }
}
