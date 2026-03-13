using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MtgCollectionTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDeckBannerCard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BannerCardId",
                table: "Decks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Decks_BannerCardId",
                table: "Decks",
                column: "BannerCardId");

            migrationBuilder.AddForeignKey(
                name: "FK_Decks_Cards_BannerCardId",
                table: "Decks",
                column: "BannerCardId",
                principalTable: "Cards",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Decks_Cards_BannerCardId",
                table: "Decks");

            migrationBuilder.DropIndex(
                name: "IX_Decks_BannerCardId",
                table: "Decks");

            migrationBuilder.DropColumn(
                name: "BannerCardId",
                table: "Decks");
        }
    }
}
