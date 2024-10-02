using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MtgCollectionTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class SfCastingCostAndOracleText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CastingCost",
                table: "ScryfallCardMetadata",
                type: "TEXT",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OracleText",
                table: "ScryfallCardMetadata",
                type: "TEXT",
                maxLength: 650,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CastingCost",
                table: "ScryfallCardMetadata");

            migrationBuilder.DropColumn(
                name: "OracleText",
                table: "ScryfallCardMetadata");
        }
    }
}
