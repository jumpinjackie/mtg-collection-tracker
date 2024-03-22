using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace MtgCollectionTracker.ViewModels;

public partial class AddCardsViewModel : DrawerContentViewModel
{
    readonly IMessenger _messenger;

    public AddCardsViewModel()
    {
        base.ThrowIfNotDesignMode();
        _messenger = WeakReferenceMessenger.Default;
    }

    public AddCardsViewModel(IMessenger messenger)
    {
        _messenger = messenger;
    }

    [RelayCommand]
    private void AddCards()
    {

    }
}
