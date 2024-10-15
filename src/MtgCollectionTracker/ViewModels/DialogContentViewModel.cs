using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Services.Messaging;
using System;
using System.Threading.Tasks;

namespace MtgCollectionTracker.ViewModels;

public abstract partial class DialogContentViewModel : RecipientViewModelBase
{
    protected DialogContentViewModel() : base() { }

    protected DialogContentViewModel(IMessenger messenger) : base(messenger) { }
}

public partial class DialogViewModel : ViewModelBase, IRecipient<GlobalBusyMessage>
{
    readonly IMessenger _messenger;

    public DialogViewModel() : this(WeakReferenceMessenger.Default)
    { }

    public DialogViewModel(IMessenger messenger)
    {
        _messenger = messenger;
    }

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private DialogContentViewModel? _contentDataContext;

    [ObservableProperty]
    private bool _isBusy;

    public DialogViewModel WithConfirmation(
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
            .WithActions(yesAction, async () => _messenger.Send(new CloseDialogMessage()));
        return this;
    }

    public DialogViewModel WithContent(string title, DialogContentViewModel dataContext)
    {
        this.Title = title;
        this.ContentDataContext = dataContext;
        return this;
    }

    [RelayCommand]
    private void Close()
    {
        _messenger.Send(new CloseDialogMessage());
    }

    void IRecipient<GlobalBusyMessage>.Receive(GlobalBusyMessage message)
    {
        this.IsBusy = message.IsBusy;
    }
}
