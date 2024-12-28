using Avalonia.Controls;
using Avalonia.Input;
using MtgCollectionTracker.ViewModels;

namespace MtgCollectionTracker.Views
{
    public partial class DeckDetailsView : UserControl
    {
        public DeckDetailsView()
        {
            InitializeComponent();
        }

        private async void CopyToClipboard_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var text = this.deckListText.Text;
            if (!string.IsNullOrEmpty(text))
            {
                var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
                if (clipboard != null)
                {
                    var dataObject = new DataObject();
                    dataObject.Set(DataFormats.Text, text);
                    await clipboard.SetDataObjectAsync(dataObject);

                    var vm = this.DataContext as DeckDetailsViewModel;
                    if (vm != null)
                    {
                        vm.DeckListCopiedToClipboard();
                    }
                }
            }
        }
    }
}
