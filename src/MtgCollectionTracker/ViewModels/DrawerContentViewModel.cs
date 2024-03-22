using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Services.Messaging;

namespace MtgCollectionTracker.ViewModels;

public partial class DrawerContentViewModel : ViewModelBase
{
    
}

public partial class DrawerViewModel : ViewModelBase
{
    readonly IMessenger _messenger;

    public DrawerViewModel()
    {
        _messenger = WeakReferenceMessenger.Default;
    }

    public DrawerViewModel(IMessenger messenger)
    {
        _messenger = messenger;
    }

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private DrawerContentViewModel _contentDataContext;

    public DrawerViewModel WithContent(string title, DrawerContentViewModel dataContext)
    {
        this.Title = title;
        this.ContentDataContext = dataContext;
        return this;
    }

    [RelayCommand]
    private void Close()
    {
        _messenger.Send(new CloseDrawerMessage());
    }
}
