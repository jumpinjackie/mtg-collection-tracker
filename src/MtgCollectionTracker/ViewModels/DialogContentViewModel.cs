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

    [ObservableProperty]
    private bool _canClose = true;

    public DialogViewModel WithConfirmation(
        string title,
        string message,
        Func<ValueTask> yesAction,
        string yesLabel = "Yes",
        string noLabel = "No")
    {
        this.Title = title;
        this.CanClose = true;
        this.ContentDataContext = new ConfirmViewModel()
            .WithMessage(message)
            .WithActionLabels(yesLabel, noLabel)
            .WithActions(yesAction, async () => _messenger.Send(new CloseDialogMessage()));
        return this;
    }

    public DialogViewModel WithContent(string title, DialogContentViewModel dataContext, bool canClose = true)
    {
        this.Title = title;
        this.CanClose = canClose;
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

    /// <summary>
    /// Configures this dialog to display the "Missing Local Price Data" notice.
    /// </summary>
    public DialogViewModel WithMissingLocalPriceDataContent()
    {
        return this.WithContent(
            "Missing Local Price Data",
            new ContainerTextViewModel().WithText(
                "No local card price data was found.\n\n" +
                "To use Lowest Price Check, open Settings, go to the Database tab, and import card prices.\n\n" +
                "After importing price data, run Lowest Price Check again."));
    }
}
