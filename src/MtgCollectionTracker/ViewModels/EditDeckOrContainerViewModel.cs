using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services;
using MtgCollectionTracker.Services.Messaging;
using MtgCollectionTracker.Services.Stubs;
using System.Threading.Tasks;

namespace MtgCollectionTracker.ViewModels;

public partial class EditDeckOrContainerViewModel : DialogContentViewModel
{
    readonly ICollectionTrackingService _service;

    public EditDeckOrContainerViewModel(IMessenger messenger, ICollectionTrackingService service)
        : base(messenger)
    {
        this.Name = string.Empty;
        _service = service;
    }

    public EditDeckOrContainerViewModel() : base()
    {
        this.ThrowIfNotDesignMode();
        this.Name = string.Empty;
        _service = new StubCollectionTrackingService();
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _name;

    [ObservableProperty]
    private string? _deckFormat;

    [ObservableProperty]
    private string? _containerDescription;

    public bool IsDeck => Type == DeckOrContainer.Deck;

    public bool IsContainer => Type == DeckOrContainer.Container;

    public DeckOrContainer Type { get; set; }

    private bool CanSave() => !string.IsNullOrWhiteSpace(this.Name);

    private int _deckOrContainerId;

    public EditDeckOrContainerViewModel WithDeck(int id, string name, string format)
    {
        _deckOrContainerId = id;
        this.Type = DeckOrContainer.Deck;
        this.Name = name;
        this.DeckFormat = format;
        return this;
    }

    public EditDeckOrContainerViewModel WithContainer(int id, string name, string? description)
    {
        _deckOrContainerId = id;
        this.Type = DeckOrContainer.Container;
        this.Name = name;
        this.ContainerDescription = description;
        return this;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task Save()
    {
        switch (this.Type)
        {
            case DeckOrContainer.Deck:
                var di = await _service.UpdateDeckAsync(_deckOrContainerId, this.Name, this.DeckFormat, null);
                this.Messenger.Send(new DeckUpdatedMessage(di));
                this.Messenger.ToastNotify($"Deck updated ({this.Name})");
                this.Messenger.Send(new CloseDialogMessage());
                break;
            case DeckOrContainer.Container:
                var ci = await _service.UpdateContainerAsync(_deckOrContainerId, this.Name, this.ContainerDescription);
                this.Messenger.Send(new ContainerUpdatedMessage(ci));
                this.Messenger.ToastNotify($"Container updated ({this.Name})");
                this.Messenger.Send(new CloseDialogMessage());
                break;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        this.Messenger.Send(new CloseDialogMessage());
    }
}
