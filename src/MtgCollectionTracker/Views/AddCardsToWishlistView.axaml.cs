using Avalonia.Controls;
using System;

namespace MtgCollectionTracker.Views
{
    public partial class AddCardsToWishlistView : UserControl
    {
        public AddCardsToWishlistView()
        {
            InitializeComponent();
            this.AttachedToVisualTree += (_, _) => UpdateGridMaxHeight();
            this.SizeChanged += (_, _) => UpdateGridMaxHeight();
        }

        private void UpdateGridMaxHeight()
        {
            // Keep the bottom action row visible by limiting DataGrid growth to available vertical space.
            var availableHeight = this.Bounds.Height
                - IntroTextBlock.Bounds.Height
                - BottomActionsGrid.Bounds.Height
                - 24;

            AddCardsDataGrid.MaxHeight = Math.Max(140, availableHeight);
        }
    }
}
