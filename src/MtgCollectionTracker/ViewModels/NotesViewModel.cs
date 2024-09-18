using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services;
using MtgCollectionTracker.Services.Stubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MtgCollectionTracker.ViewModels;

public partial class NotesViewModel : RecipientViewModelBase
{
    readonly ICollectionTrackingService _service;
    readonly IMessenger _messenger;

    public NotesViewModel(ICollectionTrackingService service, IMessenger messenger)
    {
        _service = service;
        _messenger = messenger;
        this.IsActive = true;
    }

    public NotesViewModel()
    {
        base.ThrowIfNotDesignMode();
        _service = new StubCollectionTrackingService();
        _messenger = WeakReferenceMessenger.Default;
        this.IsActive = true;
    }

    protected override void OnActivated()
    {
        if (!Avalonia.Controls.Design.IsDesignMode)
        {
            this.Notes = _service.GetNotes();
        }
        base.OnActivated();
    }

    [ObservableProperty]
    private string _notes;

    [RelayCommand]
    private async Task SaveNotes()
    {
        await _service.UpdateNotesAsync(this.Notes);
        _messenger.ToastNotify("Notes updated");
    }
}
