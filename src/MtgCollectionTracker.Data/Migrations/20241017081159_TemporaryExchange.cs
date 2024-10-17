using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MtgCollectionTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class TemporaryExchange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExchangeId",
                table: "Cards",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TemporaryExchanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ToDeckId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemporaryExchanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemporaryExchanges_Decks_ToDeckId",
                        column: x => x.ToDeckId,
                        principalTable: "Decks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExchangedDeckCard",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    CardId = table.Column<int>(type: "INTEGER", nullable: false),
                    ExchangeId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangedDeckCard", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExchangedDeckCard_Cards_CardId",
                        column: x => x.CardId,
                        principalTable: "Cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExchangedDeckCard_TemporaryExchanges_ExchangeId",
                        column: x => x.ExchangeId,
                        principalTable: "TemporaryExchanges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cards_ExchangeId",
                table: "Cards",
                column: "ExchangeId");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangedDeckCard_CardId",
                table: "ExchangedDeckCard",
                column: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangedDeckCard_ExchangeId",
                table: "ExchangedDeckCard",
                column: "ExchangeId");

            migrationBuilder.CreateIndex(
                name: "IX_TemporaryExchanges_ToDeckId",
                table: "TemporaryExchanges",
                column: "ToDeckId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cards_TemporaryExchanges_ExchangeId",
                table: "Cards",
                column: "ExchangeId",
                principalTable: "TemporaryExchanges",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cards_TemporaryExchanges_ExchangeId",
                table: "Cards");

            migrationBuilder.DropTable(
                name: "ExchangedDeckCard");

            migrationBuilder.DropTable(
                name: "TemporaryExchanges");

            migrationBuilder.DropIndex(
                name: "IX_Cards_ExchangeId",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "ExchangeId",
                table: "Cards");
        }
    }
}
