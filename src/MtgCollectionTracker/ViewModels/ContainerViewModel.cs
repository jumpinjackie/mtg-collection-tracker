using CommunityToolkit.Mvvm.ComponentModel;
using MtgCollectionTracker.Core.Model;

namespace MtgCollectionTracker.ViewModels;

public partial class ContainerViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _name = "Test Container";

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private string _total = "Total: 100 cards";

    public int Id { get; private set; }

    public ContainerViewModel WithData(ContainerSummaryModel container)
    {
        this.Id = container.Id;
        this.Name = container.Name;
        this.Description = container.Description;
        this.Total = $"Total: {container.Total} cards";
        return this;
    }
}
