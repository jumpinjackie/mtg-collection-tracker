using CommunityToolkit.Mvvm.ComponentModel;
using MtgCollectionTracker.Core.Model;
using System.Text;

namespace MtgCollectionTracker.ViewModels;

public partial class BuyingListViewModel : DialogContentViewModel
{
    [ObservableProperty]
    private string? _contents;

    public BuyingListViewModel WithText(WishlistBuyingListModel model)
    {
        var text = new StringBuilder();
        model.Write(text);
        this.Contents = text.ToString();
        return this;
    }
}
