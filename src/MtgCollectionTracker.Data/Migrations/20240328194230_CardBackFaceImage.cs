using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MtgCollectionTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class CardBackFaceImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "BackImageLarge",
                table: "ScryfallCardMetadata",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "BackImageSmall",
                table: "ScryfallCardMetadata",
                type: "BLOB",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BackImageLarge",
                table: "ScryfallCardMetadata");

            migrationBuilder.DropColumn(
                name: "BackImageSmall",
                table: "ScryfallCardMetadata");
        }
    }
}
