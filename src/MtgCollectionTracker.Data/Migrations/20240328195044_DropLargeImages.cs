using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MtgCollectionTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class DropLargeImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BackImageLarge",
                table: "ScryfallCardMetadata");

            migrationBuilder.DropColumn(
                name: "ImageLarge",
                table: "ScryfallCardMetadata");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "BackImageLarge",
                table: "ScryfallCardMetadata",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "ImageLarge",
                table: "ScryfallCardMetadata",
                type: "BLOB",
                nullable: true);
        }
    }
}
