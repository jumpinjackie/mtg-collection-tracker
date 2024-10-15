using CommunityToolkit.Mvvm.ComponentModel;

namespace MtgCollectionTracker.ViewModels;

public partial class ContainerTextViewModel : DialogContentViewModel
{
    [ObservableProperty]
    private string _text = string.Empty;

    public ContainerTextViewModel WithText(string text)
    {
        this.Text = text;
        return this;
    }
}
