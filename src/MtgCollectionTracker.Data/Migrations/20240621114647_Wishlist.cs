using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MtgCollectionTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class Wishlist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Vendors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vendors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WishlistItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    CardName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Edition = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                    LanguageId = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    CollectorNumber = table.Column<string>(type: "TEXT", maxLength: 5, nullable: true),
                    ScryfallId = table.Column<string>(type: "TEXT", nullable: true),
                    IsFoil = table.Column<bool>(type: "INTEGER", nullable: false),
                    Condition = table.Column<int>(type: "INTEGER", nullable: true),
                    IsLand = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WishlistItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WishlistItems_CardLanguage_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "CardLanguage",
                        principalColumn: "Code");
                    table.ForeignKey(
                        name: "FK_WishlistItems_ScryfallCardMetadata_ScryfallId",
                        column: x => x.ScryfallId,
                        principalTable: "ScryfallCardMetadata",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VendorPrice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    VendorId = table.Column<int>(type: "INTEGER", nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", nullable: false),
                    AvailableStock = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorPrice", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorPrice_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VendorPrice_WishlistItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "WishlistItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VendorPrice_ItemId",
                table: "VendorPrice",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorPrice_VendorId",
                table: "VendorPrice",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_WishlistItems_LanguageId",
                table: "WishlistItems",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_WishlistItems_ScryfallId",
                table: "WishlistItems",
                column: "ScryfallId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VendorPrice");

            migrationBuilder.DropTable(
                name: "Vendors");

            migrationBuilder.DropTable(
                name: "WishlistItems");
        }
    }
}
