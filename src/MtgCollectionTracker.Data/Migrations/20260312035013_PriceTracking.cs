using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MtgCollectionTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class PriceTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "TrackPrice",
                table: "Cards",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CardSkuPriceHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CardSkuId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    PriceUsd = table.Column<decimal>(type: "TEXT", nullable: true),
                    CheapestPriceUsd = table.Column<decimal>(type: "TEXT", nullable: true),
                    CheapestEdition = table.Column<string>(type: "TEXT", maxLength: 5, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardSkuPriceHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CardSkuPriceHistory_Cards_CardSkuId",
                        column: x => x.CardSkuId,
                        principalTable: "Cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CardSkuPriceHistory_CardSkuId_Date",
                table: "CardSkuPriceHistory",
                columns: new[] { "CardSkuId", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CardSkuPriceHistory");

            migrationBuilder.DropColumn(
                name: "TrackPrice",
                table: "Cards");
        }
    }
}
