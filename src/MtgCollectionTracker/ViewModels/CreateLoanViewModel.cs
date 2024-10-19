using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services;
using MtgCollectionTracker.Services.Messaging;
using MtgCollectionTracker.Services.Stubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MtgCollectionTracker.ViewModels;

public partial class CreateLoanViewModel : DialogContentViewModel
{
    readonly ICollectionTrackingService _service;

    public CreateLoanViewModel(ICollectionTrackingService service, Func<DeckViewModel> deckItem, IMessenger messenger)
        : base(messenger)
    {
        _service = service;
        this.AvailableDecks = _service.GetDecks(null).Select(deck => deckItem().WithData(deck));
    }

    public CreateLoanViewModel()
        : base(WeakReferenceMessenger.Default)
    {
        this.ThrowIfNotDesignMode();
        _service = new StubCollectionTrackingService();
        this.AvailableDecks = [
            new DeckViewModel().WithData(new() { Id = 1, Name = "My Vintage Deck"}),
            new DeckViewModel().WithData(new() { Id = 2, Name = "My Legacy Deck"}),
        ];
    }

    public IEnumerable<DeckViewModel> AvailableDecks { get; private set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _name = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private DeckViewModel? _deck;

    private bool CanSave() => this.Deck != null && !string.IsNullOrWhiteSpace(this.Name);

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task Save(CancellationToken cancel)
    {
        if (this.Deck != null)
        {
            var res = await _service.CreateLoanAsync(this.Name, this.Deck.DeckId, cancel);
            Messenger.Send(new LoanCreatedMessage(res));
            Messenger.ToastNotify($"Loan created: {res.Name}", Avalonia.Controls.Notifications.NotificationType.Success);
            Messenger.Send(new CloseDialogMessage());
        }
    }

    [RelayCommand]
    private void Cancel() => Messenger.Send(new CloseDialogMessage());
}
