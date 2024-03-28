using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Services.Messaging;
using System;
using System.Threading.Tasks;

namespace MtgCollectionTracker.ViewModels;

public abstract partial class DrawerContentViewModel : RecipientViewModelBase
{
    protected DrawerContentViewModel() : base() { }

    protected DrawerContentViewModel(IMessenger messenger) : base(messenger) { }
}

public partial class DrawerViewModel : ViewModelBase
{
    readonly IMessenger _messenger;

    public DrawerViewModel() : this(WeakReferenceMessenger.Default)
    { }

    public DrawerViewModel(IMessenger messenger)
    {
        _messenger = messenger;
    }

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private DrawerContentViewModel? _contentDataContext;

    public DrawerViewModel WithConfirmation(
        string title,
        string message,
        Func<ValueTask> yesAction,
        string yesLabel = "Yes",
        string noLabel = "No")
    {
        this.Title = title;
        this.ContentDataContext = new ConfirmViewModel()
            .WithMessage(message)
            .WithActionLabels(yesLabel, noLabel)
            .WithActions(yesAction, async () => _messenger.Send(new CloseDrawerMessage()));
        return this;
    }

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
