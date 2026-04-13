using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
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
                    await clipboard.SetTextAsync(text);

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
