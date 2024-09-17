using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services.Stubs;

namespace MtgCollectionTracker.ViewModels;

public partial class DeckListViewModel : DialogContentViewModel
{
    readonly ICollectionTrackingService _service;

    public DeckListViewModel()
    {
        base.ThrowIfNotDesignMode();
        _service = new StubCollectionTrackingService();
        this.IsActive = true;
        this.Name = "Test Deck";
        this.DeckList = """
            4 Force of Will
            4 Daze
            4 Brainstorm
            4 Ponder
            """;
    }

    public DeckListViewModel(ICollectionTrackingService service)
    {
        _service = service;
        this.IsActive = true;
    }

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string? _deckList;

    private int _deckId;

    public DeckListViewModel WithDeck(int deckId, string name)
    {
        _deckId = deckId;
        this.Name = name;
        this.DeckList = _service.PrintDeck(_deckId, false);
        return this;
    }

    [RelayCommand]
    private void ShowProxyUsage()
    {
        this.DeckList = _service.PrintDeck(_deckId, true);
    }

    [RelayCommand]
    private void HideProxyUsage()
    {
        this.DeckList = _service.PrintDeck(_deckId, false);
    }
}
