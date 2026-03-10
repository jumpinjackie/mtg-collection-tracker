using System;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Services.Messaging;

namespace MtgCollectionTracker.ViewModels;

/// <summary>
/// ViewModel for the "Add Counter" dialog that lets users add a named, colored counter to a card
/// </summary>
public partial class AddCounterViewModel : DialogContentViewModel
{
    private Action<string, Color, int>? _addAction;

    public AddCounterViewModel()
        : base() { }

    public AddCounterViewModel(IMessenger messenger)
        : base(messenger) { }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAdd))]
    private string _counterName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreviewBrush), nameof(ColorParseError))]
    private string _colorHex = "#22CC44";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAdd))]
    private int _quantity = 1;

    public IBrush PreviewBrush
    {
        get
        {
            try
            {
                return new SolidColorBrush(Color.Parse(ColorHex));
            }
            catch (FormatException)
            {
                return Brushes.Transparent;
            }
        }
    }

    public string? ColorParseError
    {
        get
        {
            try
            {
                Color.Parse(ColorHex);
                return null;
            }
            catch (FormatException)
            {
                return "Invalid color. Use hex format like #FF0000 or a named color.";
            }
        }
    }

    private bool IsColorValid
    {
        get
        {
            try { Color.Parse(ColorHex); return true; }
            catch (FormatException) { return false; }
        }
    }

    public bool CanAdd => !string.IsNullOrWhiteSpace(CounterName) && Quantity > 0 && IsColorValid;

    public AddCounterViewModel Configure(Action<string, Color, int> addAction)
    {
        _addAction = addAction;
        return this;
    }

    [RelayCommand(CanExecute = nameof(CanAdd))]
    private void Add()
    {
        if (_addAction is null || string.IsNullOrWhiteSpace(CounterName))
            return;

        Color color;
        try { color = Color.Parse(ColorHex); }
        catch (FormatException) { return; }

        _addAction(CounterName.Trim(), color, Quantity);
        Messenger.Send(new CloseDialogMessage());
    }

    [RelayCommand]
    private void Cancel()
    {
        Messenger.Send(new CloseDialogMessage());
    }

    partial void OnCounterNameChanged(string value)
    {
        OnPropertyChanged(nameof(CanAdd));
        AddCommand.NotifyCanExecuteChanged();
    }

    partial void OnQuantityChanged(int value)
    {
        OnPropertyChanged(nameof(CanAdd));
        AddCommand.NotifyCanExecuteChanged();
    }

    partial void OnColorHexChanged(string value)
    {
        OnPropertyChanged(nameof(CanAdd));
        AddCommand.NotifyCanExecuteChanged();
    }
}
