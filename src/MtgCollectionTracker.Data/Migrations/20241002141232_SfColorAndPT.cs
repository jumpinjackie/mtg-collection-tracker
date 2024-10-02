using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MtgCollectionTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class SfColorAndPT : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ColorIdentity",
                table: "ScryfallCardMetadata",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Colors",
                table: "ScryfallCardMetadata",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Power",
                table: "ScryfallCardMetadata",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Toughness",
                table: "ScryfallCardMetadata",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ColorIdentity",
                table: "ScryfallCardMetadata");

            migrationBuilder.DropColumn(
                name: "Colors",
                table: "ScryfallCardMetadata");

            migrationBuilder.DropColumn(
                name: "Power",
                table: "ScryfallCardMetadata");

            migrationBuilder.DropColumn(
                name: "Toughness",
                table: "ScryfallCardMetadata");
        }
    }
}
