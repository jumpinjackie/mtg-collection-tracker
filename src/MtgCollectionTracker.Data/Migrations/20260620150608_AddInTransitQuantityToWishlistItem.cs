using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MtgCollectionTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInTransitQuantityToWishlistItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InTransitQuantity",
                table: "WishlistItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InTransitQuantity",
                table: "WishlistItems");
        }
    }
}
