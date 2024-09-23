using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MtgCollectionTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class VendorOfferNotesAndLargeImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "VendorPrice",
                type: "TEXT",
                nullable: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "VendorPrice");

            migrationBuilder.DropColumn(
                name: "BackImageLarge",
                table: "ScryfallCardMetadata");

            migrationBuilder.DropColumn(
                name: "ImageLarge",
                table: "ScryfallCardMetadata");
        }
    }
}
