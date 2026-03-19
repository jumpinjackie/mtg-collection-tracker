using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MtgCollectionTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCommanderSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScryfallCardMetadata_ScryfallIdMappings_Id",
                table: "ScryfallCardMetadata");

            migrationBuilder.AddColumn<Guid>(
                name: "CommanderId",
                table: "Decks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCommander",
                table: "Decks",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Decks_CommanderId",
                table: "Decks",
                column: "CommanderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Decks_Cards_CommanderId",
                table: "Decks",
                column: "CommanderId",
                principalTable: "Cards",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Decks_Cards_CommanderId",
                table: "Decks");

            migrationBuilder.DropIndex(
                name: "IX_Decks_CommanderId",
                table: "Decks");

            migrationBuilder.DropColumn(
                name: "CommanderId",
                table: "Decks");

            migrationBuilder.DropColumn(
                name: "IsCommander",
                table: "Decks");

            //migrationBuilder.AddForeignKey(
            //    name: "FK_ScryfallCardMetadata_ScryfallIdMappings_Id",
            //    table: "ScryfallCardMetadata",
            //    column: "Id",
            //    principalTable: "ScryfallIdMappings",
            //    principalColumn: "ScryfallId");
        }
    }
}
