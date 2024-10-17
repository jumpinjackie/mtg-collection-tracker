using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services;
using MtgCollectionTracker.Services.Messaging;
using MtgCollectionTracker.Services.Stubs;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace MtgCollectionTracker.ViewModels;

public partial class TempExchangesViewModel : RecipientViewModelBase, IRecipient<LoanCreatedMessage>, IRecipient<LoanDeletedMessage>
{
    readonly ICollectionTrackingService _service;
    readonly Func<DialogViewModel> _dialog;
    readonly Func<CreateLoanViewModel> _create;

    public TempExchangesViewModel()
    {
        _service = new StubCollectionTrackingService();
        _dialog = () => new();
        _create = () => new();
        this.IsActive = true;
        this.Exchanges.CollectionChanged += Exchanges_CollectionChanged;
    }

    public TempExchangesViewModel(ICollectionTrackingService service,
                                  Func<DialogViewModel> dialog,
                                  Func<CreateLoanViewModel> create)
    {
        _service = service;
        _dialog = dialog;
        _create = create;
        this.IsActive = true;
        this.Exchanges.CollectionChanged += Exchanges_CollectionChanged;
    }

    private void Exchanges_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        this.OnPropertyChanged(nameof(IsEmptyCollection));
    }

    protected override void OnActivated()
    {
        if (!Avalonia.Controls.Design.IsDesignMode)
        {
            this.RefreshListCommand.Execute(null);
        }
        base.OnActivated();
    }

    public ObservableCollection<TempExchangeViewModel> Exchanges { get; } = new();

    public bool IsEmptyCollection => Exchanges.Count == 0;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(EditCommand))]
    private TempExchangeViewModel? _selectedExchange;

    [RelayCommand]
    private void Create()
    {
        Messenger.Send(new OpenDialogMessage
        {
            DrawerWidth = 800,
            ViewModel = _dialog().WithContent("Create New Loan", _create())
        });
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void Edit()
    {
        if (this.SelectedExchange != null)
        {
            
        }
    }

    private bool HasSelection() => this.SelectedExchange != null;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void Delete()
    {
        if (this.SelectedExchange != null)
        {
            Messenger.Send(new OpenDialogMessage
            {
                DrawerWidth = 800,
                ViewModel = _dialog().WithConfirmation("Delete Loan", $"Are you sure you want to delete this loan ({this.SelectedExchange.Name})?\n\nDoing so will return the given cards back to their respective decks and containers.", async () =>
                {
                    var res = await _service.DeleteLoanAsync(this.SelectedExchange.Id, CancellationToken.None);
                    Messenger.Send(new LoanDeletedMessage(res));
                    Messenger.ToastNotify($"Loan deleted ({res.Name})");
                })
            });
        }
    }

    [RelayCommand]
    private void RefreshList()
    {
        this.Exchanges.Clear();
        var loans = _service.GetLoans();
        foreach (var loan in loans)
        {
            this.Exchanges.Add(new TempExchangeViewModel().WithData(loan));
        }
    }

    void IRecipient<LoanCreatedMessage>.Receive(LoanCreatedMessage message)
    {
        var vm = new TempExchangeViewModel().WithData(message.Loan);
        this.Exchanges.Add(vm);
    }

    void IRecipient<LoanDeletedMessage>.Receive(LoanDeletedMessage message)
    {
        var toRemove = this.Exchanges.FirstOrDefault(e => e.Id == message.Loan.Id);
        if (toRemove != null)
        {
            this.Exchanges.Remove(toRemove);
        }
    }
}
