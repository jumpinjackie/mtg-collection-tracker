using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace MtgCollectionTracker.ViewModels;

public partial class CardsViewModel : ViewModelBase
{
    [ObservableProperty]
    private string? _searchText;

    [ObservableProperty]
    private bool _isBusy = false;

    //private CancellationTokenSource? _cancellationTokenSource;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedCardSku))]
    private CardSkuItemViewModel? _selectedCardSku;

    public bool HasSelectedCardSku => SelectedCardSku != null;

    public ObservableCollection<CardSkuItemViewModel> SearchResults { get; } = new();

    [RelayCommand]
    private async Task PerformSearch()
    {
        this.IsBusy = true;
        try
        {
            await Task.Delay(1000);
            this.SearchResults.Clear();
            this.SearchResults.Add(new CardSkuItemViewModel { CardName = "Black Lotus", Edition = "2ED", Quantity = 1 });
            this.SearchResults.Add(new CardSkuItemViewModel { CardName = "Mox Pearl", Edition = "2ED", Quantity = 1 });
            this.SearchResults.Add(new CardSkuItemViewModel { CardName = "Mox Emerald", Edition = "2ED", Quantity = 1 });
            this.SearchResults.Add(new CardSkuItemViewModel { CardName = "Mox Ruby", Edition = "2ED", Quantity = 1 });
            this.SearchResults.Add(new CardSkuItemViewModel { CardName = "Mox Jet", Edition = "2ED", Quantity = 1 });
            this.SearchResults.Add(new CardSkuItemViewModel { CardName = "Mox Sapphire", Edition = "2ED", Quantity = 1 });
            this.SearchResults.Add(new CardSkuItemViewModel { CardName = "Ancestral Recall", Edition = "2ED", Quantity = 1 });
            this.SearchResults.Add(new CardSkuItemViewModel { CardName = "Time Walk", Edition = "2ED", Quantity = 1 });
            this.SearchResults.Add(new CardSkuItemViewModel { CardName = "Timetwister", Edition = "2ED", Quantity = 1 });
        }
        finally
        {
            this.IsBusy = false;
        }
    }

    [RelayCommand]
    private void ViewCardSku()
    {

    }
}