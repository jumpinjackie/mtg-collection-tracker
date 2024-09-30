using Avalonia.Media.Imaging;
using MtgCollectionTracker.Data;

namespace MtgCollectionTracker.ViewModels;

public class CardVisualViewModel : ViewModelBase, IDeckPrintableSlot
{
    public required string CardName { get; set; }

    public string Tooltip => IsProxy ? ("PROXY: " + CardName) : CardName;

    public Bitmap? CardImage { get; set; }

    public bool IsLand { get; set; }

    public int Quantity { get; set; }

    public string? Type { get; set; }

    public bool IsProxy { get; set; }

    public bool IsGrouped { get; set; }

    public string CardNameBgColor => IsProxy ? "RosyBrown" : "Gray";

    public bool IsSideboard { get; set; }

    public required string Edition { get; set; }
}
