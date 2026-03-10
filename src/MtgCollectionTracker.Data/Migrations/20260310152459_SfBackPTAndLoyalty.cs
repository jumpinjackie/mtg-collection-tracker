using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MtgCollectionTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class SfBackPTAndLoyalty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BackLoyalty",
                table: "ScryfallCardMetadata",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BackPower",
                table: "ScryfallCardMetadata",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BackToughness",
                table: "ScryfallCardMetadata",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Loyalty",
                table: "ScryfallCardMetadata",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BackLoyalty",
                table: "ScryfallCardMetadata");

            migrationBuilder.DropColumn(
                name: "BackPower",
                table: "ScryfallCardMetadata");

            migrationBuilder.DropColumn(
                name: "BackToughness",
                table: "ScryfallCardMetadata");

            migrationBuilder.DropColumn(
                name: "Loyalty",
                table: "ScryfallCardMetadata");
        }
    }
}
