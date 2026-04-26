using Avalonia;
using Avalonia.Controls;

namespace MtgCollectionTracker.Views;

public partial class EditCardSkuView : UserControl
{
    public EditCardSkuView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Keeps <see cref="RootGrid"/>'s MaxHeight equal to the UserControl's arranged height so the
    /// scrollable form body receives a finite constraint and the action row remains reachable on
    /// shorter displays.
    /// </summary>
    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        if (e.NewSize.Height > 0 && !double.IsInfinity(e.NewSize.Height))
        {
            RootGrid.MaxHeight = e.NewSize.Height;
        }
    }
}
