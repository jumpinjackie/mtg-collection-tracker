using CommunityToolkit.Mvvm.ComponentModel;
using MtgCollectionTracker.Core.Model;

namespace MtgCollectionTracker.ViewModels;

public partial class NotesItemViewModel : ObservableObject
{
    public int? Id { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TitleText))]
    private string? _title;

    [ObservableProperty]
    private string _notes = string.Empty;

    public string TitleText => $"{(IsDirty ? "* " : string.Empty)}{Title ?? "(Untitled)"}";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TitleText))]
    private bool _isDirty;

    private string? _origTitle;
    private string _origText = string.Empty;

    partial void OnTitleChanged(string? value)
    {
        this.IsDirty = _origTitle != value
            || _origText != this.Notes;
    }

    partial void OnNotesChanged(string value)
    {
        this.IsDirty = _origTitle != this.Title
            || _origText != value;
    }

    internal NotesItemViewModel From(NotesModel n)
    {
        _origText = n.Notes;
        _origTitle = n.Title;

        this.Id = n.Id;
        this.Title = n.Title;
        this.Notes = n.Notes;
        this.IsDirty = false;
        return this;
    }
}
