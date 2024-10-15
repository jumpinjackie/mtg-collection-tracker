using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services;
using MtgCollectionTracker.Services.Contracts;
using MtgCollectionTracker.Services.Messaging;
using MtgCollectionTracker.Services.Stubs;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web;

namespace MtgCollectionTracker.ViewModels;

public partial class NotesViewModel : RecipientViewModelBase
{
    readonly IViewModelFactory _vmFactory;
    readonly ICollectionTrackingService _service;
    readonly IMessenger _messenger;

    public ObservableCollection<NotesItemViewModel> Notes { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDelete))]
    private NotesItemViewModel? _selectedNote;

    public bool CanDelete => this.SelectedNote != null;

    partial void OnSelectedNoteChanged(NotesItemViewModel? oldValue, NotesItemViewModel? newValue)
    {
        if (oldValue != null)
            oldValue.PropertyChanged -= OnNotePropertyChanged;
        if (newValue != null)
            newValue.PropertyChanged += OnNotePropertyChanged;
    }

    private void OnNotePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (SelectedNote != null && e.PropertyName == nameof(NotesItemViewModel.IsDirty))
            this.CanSave = SelectedNote.IsDirty;
    }

    [RelayCommand]
    private void AddNewNote()
    {
        var n = new NotesItemViewModel(); 
        this.Notes.Add(n);
        this.SelectedNote = n;
    }

    [RelayCommand]
    private void DeleteSelectedNote()
    {
        if (this.SelectedNote != null)
        {
            Messenger.Send(new OpenDialogMessage
            {
                DrawerWidth = 400,
                ViewModel = _vmFactory.Dialog().WithConfirmation(
                    "Delete Note",
                    $"Are you sure you want to delete this note?",
                    async () =>
                    {
                        if (this.SelectedNote.Id.HasValue)
                        {
                            await _service.DeleteNotesAsync(this.SelectedNote.Id.Value);
                        }
//                        await _service.DeleteWishlistItemAsync(item.Id);
                        Messenger.ToastNotify($"Note ({this.SelectedNote.TitleText}) deleted");
                        
                        this.Notes.Remove(this.SelectedNote);
                        this.SelectedNote = null;
                    })
            });
        }
    }

    public NotesViewModel(ICollectionTrackingService service, IViewModelFactory vmFactory, IMessenger messenger)
    {
        _service = service;
        _vmFactory = vmFactory;
        _messenger = messenger;
        this.IsActive = true;
    }

    public NotesViewModel()
    {
        base.ThrowIfNotDesignMode();
        _service = new StubCollectionTrackingService();
        _vmFactory = new StubViewModelFactory();
        _messenger = WeakReferenceMessenger.Default;
        this.IsActive = true;
    }

    protected override void OnActivated()
    {
        if (!Avalonia.Controls.Design.IsDesignMode)
        {
            this.Notes.Clear();
            foreach (var n in _service.GetNotes())
            {
                this.Notes.Add(new NotesItemViewModel().From(n));
            }
        }
        base.OnActivated();
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveNotesCommand))]
    private bool _canSave = false;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveNotes()
    {
        if (this.SelectedNote != null)
        {
            var updated = await _service.UpdateNotesAsync(this.SelectedNote.Id, this.SelectedNote.Title, this.SelectedNote.Notes);
            this.SelectedNote.From(updated);
            _messenger.ToastNotify("Notes updated");
        }
    }
}
