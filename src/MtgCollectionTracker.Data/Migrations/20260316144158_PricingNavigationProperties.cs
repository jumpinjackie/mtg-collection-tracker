using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MtgCollectionTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class PricingNavigationProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ScryfallIdMappings_MtgJsonUuid",
                table: "ScryfallIdMappings");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_ScryfallIdMappings_MtgJsonUuid",
                table: "ScryfallIdMappings",
                column: "MtgJsonUuid");

            migrationBuilder.CreateIndex(
                name: "IX_ScryfallIdMappings_MtgJsonUuid",
                table: "ScryfallIdMappings",
                column: "MtgJsonUuid",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CardPricingEntries_ScryfallIdMappings_Uuid",
                table: "CardPricingEntries",
                column: "Uuid",
                principalTable: "ScryfallIdMappings",
                principalColumn: "MtgJsonUuid");

            // Intentionally avoid creating a hard FK for this relationship.
            // Existing databases can have Scryfall metadata rows before card identifier
            // mappings are imported, and enforcing this constraint would fail migration.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CardPricingEntries_ScryfallIdMappings_Uuid",
                table: "CardPricingEntries");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_ScryfallIdMappings_MtgJsonUuid",
                table: "ScryfallIdMappings");

            migrationBuilder.DropIndex(
                name: "IX_ScryfallIdMappings_MtgJsonUuid",
                table: "ScryfallIdMappings");

            migrationBuilder.CreateIndex(
                name: "IX_ScryfallIdMappings_MtgJsonUuid",
                table: "ScryfallIdMappings",
                column: "MtgJsonUuid");
        }
    }
}
