using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MtgCollectionTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class TemporaryLoans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LoanExchangeId",
                table: "Cards",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Loans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ToDeckId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Loans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Loans_Decks_ToDeckId",
                        column: x => x.ToDeckId,
                        principalTable: "Decks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LoanExchange",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CardId = table.Column<int>(type: "INTEGER", nullable: false),
                    LoanId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanExchange", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoanExchange_Cards_CardId",
                        column: x => x.CardId,
                        principalTable: "Cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LoanExchange_Loans_LoanId",
                        column: x => x.LoanId,
                        principalTable: "Loans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cards_LoanExchangeId",
                table: "Cards",
                column: "LoanExchangeId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanExchange_CardId",
                table: "LoanExchange",
                column: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanExchange_LoanId",
                table: "LoanExchange",
                column: "LoanId");

            migrationBuilder.CreateIndex(
                name: "IX_Loans_ToDeckId",
                table: "Loans",
                column: "ToDeckId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cards_LoanExchange_LoanExchangeId",
                table: "Cards",
                column: "LoanExchangeId",
                principalTable: "LoanExchange",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cards_LoanExchange_LoanExchangeId",
                table: "Cards");

            migrationBuilder.DropTable(
                name: "LoanExchange");

            migrationBuilder.DropTable(
                name: "Loans");

            migrationBuilder.DropIndex(
                name: "IX_Cards_LoanExchangeId",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "LoanExchangeId",
                table: "Cards");
        }
    }
}
