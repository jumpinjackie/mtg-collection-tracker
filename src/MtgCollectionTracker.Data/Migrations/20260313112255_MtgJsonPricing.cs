using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MtgCollectionTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class MtgJsonPricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CardPricingDownloadHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Sha256 = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardPricingDownloadHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CardPricingEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Uuid = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    CardFinish = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    GameAvailability = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Price = table.Column<double>(type: "REAL", nullable: false),
                    PriceProvider = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ProviderListing = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardPricingEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScryfallIdMappings",
                columns: table => new
                {
                    ScryfallId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    MtgJsonUuid = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScryfallIdMappings", x => x.ScryfallId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CardPricingEntries_Uuid",
                table: "CardPricingEntries",
                column: "Uuid");

            migrationBuilder.CreateIndex(
                name: "IX_CardPricingEntries_Uuid_CardFinish_Currency_ProviderListing",
                table: "CardPricingEntries",
                columns: new[] { "Uuid", "CardFinish", "Currency", "ProviderListing" });

            migrationBuilder.CreateIndex(
                name: "IX_ScryfallIdMappings_MtgJsonUuid",
                table: "ScryfallIdMappings",
                column: "MtgJsonUuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CardPricingDownloadHistory");

            migrationBuilder.DropTable(
                name: "CardPricingEntries");

            migrationBuilder.DropTable(
                name: "ScryfallIdMappings");
        }
    }
}
