using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MtgCollectionTracker.ViewModels;

public partial class CardSkuItemViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _cardName = "CARDNAME";

    [ObservableProperty]
    private string _edition = "ED";

    [ObservableProperty]
    private string? _language = "EN";

    [ObservableProperty]
    private int _quantity = 1;

    [ObservableProperty]
    private Bitmap? _cardImage;
}