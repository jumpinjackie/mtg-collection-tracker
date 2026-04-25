using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Services;
using MtgCollectionTracker.ViewModels;
using System;

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
                    try
                    {
                        await clipboard.SetTextAsync(text);
                    }
                    catch (Exception ex) when (DesktopIntegrationExceptionHelper.IsServiceUnavailable(ex))
                    {
                        WeakReferenceMessenger.Default.ToastNotify("Clipboard access is unavailable in this desktop session.", Avalonia.Controls.Notifications.NotificationType.Warning);
                        return;
                    }

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
