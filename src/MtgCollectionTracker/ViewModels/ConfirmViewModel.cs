using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Services.Messaging;
using System;
using System.Threading.Tasks;

namespace MtgCollectionTracker.ViewModels;

public partial class ConfirmViewModel : DialogContentViewModel
{
    public ConfirmViewModel()
    {
        this.CanExecuteNo = true;
        this.CanExecuteYes = true;
    }

    private Func<ValueTask>? _yesAction;
    private Func<ValueTask>? _noAction;

    public ConfirmViewModel WithMessage(string message)
    {
        this.Message = message;
        return this;
    }

    public ConfirmViewModel WithActionLabels(string yesLabel, string noLabel)
    {
        this.YesLabel = yesLabel;
        this.NoLabel = noLabel;
        return this;
    }

    public ConfirmViewModel WithActions(Func<ValueTask> yesAction, Func<ValueTask> noAction)
    {
        _yesAction = yesAction;
        _noAction = noAction;
        return this;
    }

    [ObservableProperty]
    private string _message = "";

    [ObservableProperty]
    private string _yesLabel = "Yes";

    [ObservableProperty]
    private string _noLabel = "No";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(YesActionCommand))]
    private bool _canExecuteYes;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NoActionCommand))]
    private bool _canExecuteNo;

    [RelayCommand(CanExecute = nameof(CanExecuteYes))]
    private async Task YesAction()
    {
        if (_yesAction != null)
        {
            this.CanExecuteYes = false;
            await _yesAction.Invoke();
            this.CanExecuteYes = true;
            this.Messenger.Send(new CloseDialogMessage());
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteNo))]
    private async Task NoAction()
    {
        if (_noAction != null)
        {
            this.CanExecuteNo = false;
            await _noAction.Invoke();
            this.CanExecuteNo = true;
            this.Messenger.Send(new CloseDialogMessage());
        }
    }
}
