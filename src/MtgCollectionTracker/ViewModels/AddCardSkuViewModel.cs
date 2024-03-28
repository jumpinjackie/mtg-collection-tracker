using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MtgCollectionTracker.Data;

namespace MtgCollectionTracker.ViewModels;

public partial class AddCardSkuViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    [NotifyCanExecuteChangedFor(nameof(AddCardsCommand))]
    private int _qty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    [NotifyCanExecuteChangedFor(nameof(AddCardsCommand))]
    private string _cardName;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    [NotifyCanExecuteChangedFor(nameof(AddCardsCommand))]
    private string _edition;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    [NotifyCanExecuteChangedFor(nameof(AddCardsCommand))]
    private string? _language;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    [NotifyCanExecuteChangedFor(nameof(AddCardsCommand))]
    private string? _collectorNumber;

    [ObservableProperty]
    private bool _isFoil;

    [ObservableProperty]
    private CardCondition? _condition;

    [ObservableProperty]
    private string? _comments;

    public bool IsValid => this.Qty > 0 && !string.IsNullOrEmpty(this.CardName) && !string.IsNullOrEmpty(this.Edition);

    /// <summary>
    /// Reference copy of the root view model command so we can re-evaluate executability from this item
    /// </summary>
    public required IAsyncRelayCommand AddCardsCommand { get; set; }
}
