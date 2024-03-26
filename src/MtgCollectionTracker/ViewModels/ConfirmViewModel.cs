using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;

namespace MtgCollectionTracker.ViewModels;

public partial class ConfirmViewModel : DrawerContentViewModel
{
    public ConfirmViewModel()
    {

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

    [RelayCommand]
    private async Task YesAction()
    {
        if (_yesAction != null)
        {
            await _yesAction.Invoke();
        }
    }

    [RelayCommand]
    private async Task NoAction()
    {
        if (_noAction != null)
        {
            await _noAction.Invoke();
        }
    }
}
