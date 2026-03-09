using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Services.Messaging;

namespace MtgCollectionTracker.ViewModels;

public partial class CreateTokenViewModel : DialogContentViewModel
{
    private Action<string, string?, string, string>? _createAction;

    public CreateTokenViewModel()
        : base() { }

    public CreateTokenViewModel(IMessenger messenger)
        : base(messenger) { }

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string? _oracleText;

    [ObservableProperty]
    private string _powerToughness = "1/1";

    [ObservableProperty]
    private string? _validationMessage;

    public bool CanCreate =>
        !string.IsNullOrWhiteSpace(Name) && TryParsePowerToughness(PowerToughness, out _, out _);

    public CreateTokenViewModel Configure(Action<string, string?, string, string> createAction)
    {
        _createAction = createAction;
        RefreshValidation();
        return this;
    }

    [RelayCommand(CanExecute = nameof(CanCreate))]
    private void Create()
    {
        if (_createAction is null)
        {
            return;
        }

        if (!TryParsePowerToughness(PowerToughness, out var power, out var toughness))
        {
            RefreshValidation();
            return;
        }

        _createAction(
            Name.Trim(),
            string.IsNullOrWhiteSpace(OracleText) ? null : OracleText.Trim(),
            power,
            toughness
        );
        Messenger.Send(new CloseDialogMessage());
    }

    [RelayCommand]
    private void Cancel()
    {
        Messenger.Send(new CloseDialogMessage());
    }

    partial void OnNameChanged(string value)
    {
        RefreshValidation();
    }

    partial void OnPowerToughnessChanged(string value)
    {
        RefreshValidation();
    }

    private void RefreshValidation()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ValidationMessage = "Name is required.";
        }
        else if (!TryParsePowerToughness(PowerToughness, out _, out _))
        {
            ValidationMessage = "P/T must be in the format power/toughness (for example 1/1).";
        }
        else
        {
            ValidationMessage = null;
        }

        OnPropertyChanged(nameof(CanCreate));
        CreateCommand.NotifyCanExecuteChanged();
    }

    private static bool TryParsePowerToughness(
        string? value,
        out string power,
        out string toughness
    )
    {
        power = string.Empty;
        toughness = string.Empty;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var parts = value.Split('/', 2, StringSplitOptions.TrimEntries);
        if (
            parts.Length != 2
            || string.IsNullOrWhiteSpace(parts[0])
            || string.IsNullOrWhiteSpace(parts[1])
        )
        {
            return false;
        }

        power = parts[0];
        toughness = parts[1];
        return true;
    }
}
