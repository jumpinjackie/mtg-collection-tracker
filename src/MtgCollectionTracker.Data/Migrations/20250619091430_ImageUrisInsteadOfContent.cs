using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MtgCollectionTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class ImageUrisInsteadOfContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BackImageLarge",
                table: "ScryfallCardMetadata");

            migrationBuilder.DropColumn(
                name: "BackImageSmall",
                table: "ScryfallCardMetadata");

            migrationBuilder.DropColumn(
                name: "ImageLarge",
                table: "ScryfallCardMetadata");

            migrationBuilder.DropColumn(
                name: "ImageSmall",
                table: "ScryfallCardMetadata");

            migrationBuilder.AddColumn<string>(
                name: "BackImageLargeUrl",
                table: "ScryfallCardMetadata",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BackImageSmallUrl",
                table: "ScryfallCardMetadata",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageLargeUrl",
                table: "ScryfallCardMetadata",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageSmallUrl",
                table: "ScryfallCardMetadata",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BackImageLargeUrl",
                table: "ScryfallCardMetadata");

            migrationBuilder.DropColumn(
                name: "BackImageSmallUrl",
                table: "ScryfallCardMetadata");

            migrationBuilder.DropColumn(
                name: "ImageLargeUrl",
                table: "ScryfallCardMetadata");

            migrationBuilder.DropColumn(
                name: "ImageSmallUrl",
                table: "ScryfallCardMetadata");

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

            migrationBuilder.AddColumn<byte[]>(
                name: "ImageLarge",
                table: "ScryfallCardMetadata",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "ImageSmall",
                table: "ScryfallCardMetadata",
                type: "BLOB",
                nullable: true);
        }
    }
}
