using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services;
using MtgCollectionTracker.Services.Messaging;
using MtgCollectionTracker.Services.Stubs;
using System;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MtgCollectionTracker.ViewModels;

public partial class TagViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;
}

public partial class SettingsViewModel : RecipientViewModelBase
{
    readonly ICollectionTrackingService _service;

    public SettingsViewModel(ICollectionTrackingService service, IMessenger messenger)
        : base(messenger)
    {
        _service = service;
        this.IsActive = true;
    }

    public SettingsViewModel()
        : base(WeakReferenceMessenger.Default)
    {
        this.ThrowIfNotDesignMode();
        _service = new StubCollectionTrackingService();
        this.IsActive = true;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        if (Avalonia.Controls.Design.IsDesignMode)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Foo");
            sb.AppendLine("Bar");
            sb.AppendLine("Baz");
            var t = sb.ToString();
            _origTags = t;
            this.Tags = t;
        }
        else
        {
            var t = string.Join(Environment.NewLine, _service.GetTags()); ;
            _origTags = t;
            this.Tags = t;
        }
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveTagsCommand))]
    private string _tags;

    private string _origTags;

    private bool CanSaveTags() => _origTags != this.Tags;

    [RelayCommand(CanExecute = nameof(CanSaveTags))]
    private async Task SaveTags(CancellationToken cancel)
    {
        var tags = this.Tags.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var res = await _service.ApplyTagsAsync(tags, cancel);

        Messenger.ToastNotify($"Tags applied ({res.Added} added, {res.Deleted} deleted, {res.Detached} detached)");
        Messenger.Send(new TagsAppliedMessage { CurrentTags = res.CurrentTags });

        var t = string.Join(Environment.NewLine, tags);
        _origTags = t;
        this.Tags = t;
    }
}
