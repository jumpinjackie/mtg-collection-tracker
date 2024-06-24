using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MtgCollectionTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class ExtraScryfallMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ManaValue",
                table: "ScryfallCardMetadata",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "ScryfallCardMetadata",
                type: "TEXT",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ManaValue",
                table: "ScryfallCardMetadata");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "ScryfallCardMetadata");
        }
    }
}
