using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services;
using MtgCollectionTracker.Services.Messaging;
using MtgCollectionTracker.Services.Stubs;
using System.Threading.Tasks;

namespace MtgCollectionTracker.ViewModels;

public enum DeckOrContainer
{
    Deck,
    Container
}

public partial class NewDeckOrContainerViewModel : DrawerContentViewModel
{
    readonly ICollectionTrackingService _service;

    public NewDeckOrContainerViewModel(IMessenger messenger, ICollectionTrackingService service)
        : base(messenger)
    {
        this.Name = string.Empty;
        _service = service;
    }

    public NewDeckOrContainerViewModel() : base()
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

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task Save() 
    {
        switch (this.Type)
        {
            case DeckOrContainer.Deck:
                var di = await _service.CreateDeckAsync(this.Name, this.DeckFormat, null);
                this.Messenger.Send(new DeckCreatedMessage { Deck = di });
                this.Messenger.ToastNotify($"Deck created ({this.Name})");
                this.Messenger.Send(new CloseDrawerMessage());
                break;
            case DeckOrContainer.Container:
                var ci = await _service.CreateContainerAsync(this.Name, this.ContainerDescription);
                this.Messenger.Send(new ContainerCreatedMessage { Container = ci });
                this.Messenger.ToastNotify($"Container created ({this.Name})");
                this.Messenger.Send(new CloseDrawerMessage());
                break;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        this.Messenger.Send(new CloseDrawerMessage());
    }
}
