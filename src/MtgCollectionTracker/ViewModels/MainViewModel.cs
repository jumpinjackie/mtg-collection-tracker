namespace MtgCollectionTracker.ViewModels;

public class MainViewModel : ViewModelBase
{
    public CardsViewModel Cards { get; } = new();
}