using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MtgCollectionTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class CardLanguage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Language",
                table: "Cards",
                newName: "LanguageId");

            migrationBuilder.CreateTable(
                name: "CardLanguage",
                columns: table => new
                {
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    PrintedCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardLanguage", x => x.Code);
                });

            migrationBuilder.InsertData(
                table: "CardLanguage",
                columns: new[] { "Code", "Name", "PrintedCode" },
                values: new object[,]
                {
                    { "ar", "Arabic", null },
                    { "de", "German", "de" },
                    { "en", "English", "en" },
                    { "es", "Spanish", "sp" },
                    { "fr", "French", "fr" },
                    { "grc", "Ancient Greek", null },
                    { "he", "Hebrew", null },
                    { "it", "Italian", "it" },
                    { "ja", "Japanese", "jp" },
                    { "ko", "Korean", "kr" },
                    { "la", "Latin", null },
                    { "ph", "Phyrexian", "ph" },
                    { "pt", "Portuguese", "pt" },
                    { "ru", "Russian", "ru" },
                    { "sa", "Sanskrit", null },
                    { "zhs", "Simplified Chinese", "cs" },
                    { "zht", "Traditional Chinese", "ct" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cards_LanguageId",
                table: "Cards",
                column: "LanguageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cards_CardLanguage_LanguageId",
                table: "Cards",
                column: "LanguageId",
                principalTable: "CardLanguage",
                principalColumn: "Code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cards_CardLanguage_LanguageId",
                table: "Cards");

            migrationBuilder.DropTable(
                name: "CardLanguage");

            migrationBuilder.DropIndex(
                name: "IX_Cards_LanguageId",
                table: "Cards");

            migrationBuilder.RenameColumn(
                name: "LanguageId",
                table: "Cards",
                newName: "Language");
        }
    }
}
