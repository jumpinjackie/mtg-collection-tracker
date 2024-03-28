using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MtgCollectionTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class ScryfallCardMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CollectorNumber",
                table: "Cards",
                type: "TEXT",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScryfallId",
                table: "Cards",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ScryfallCardMetadata",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    CardName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Edition = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                    CardType = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Rarity = table.Column<string>(type: "TEXT", maxLength: 11, nullable: false),
                    CollectorNumber = table.Column<string>(type: "TEXT", maxLength: 5, nullable: true),
                    Language = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    ImageLarge = table.Column<byte[]>(type: "BLOB", nullable: true),
                    ImageSmall = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScryfallCardMetadata", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cards_ScryfallId",
                table: "Cards",
                column: "ScryfallId");

            migrationBuilder.CreateIndex(
                name: "IX_ScryfallCardMetadata_CardName_Edition_Language_CollectorNumber",
                table: "ScryfallCardMetadata",
                columns: new[] { "CardName", "Edition", "Language", "CollectorNumber" });

            migrationBuilder.AddForeignKey(
                name: "FK_Cards_ScryfallCardMetadata_ScryfallId",
                table: "Cards",
                column: "ScryfallId",
                principalTable: "ScryfallCardMetadata",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cards_ScryfallCardMetadata_ScryfallId",
                table: "Cards");

            migrationBuilder.DropTable(
                name: "ScryfallCardMetadata");

            migrationBuilder.DropIndex(
                name: "IX_Cards_ScryfallId",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "CollectorNumber",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "ScryfallId",
                table: "Cards");
        }
    }
}
