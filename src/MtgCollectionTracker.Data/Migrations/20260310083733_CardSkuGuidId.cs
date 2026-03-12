using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MtgCollectionTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class CardSkuGuidId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add a temporary NewId TEXT column to Cards and populate with UUIDs
            migrationBuilder.Sql(@"ALTER TABLE ""Cards"" ADD COLUMN ""NewId"" TEXT");

            // Generate UUID v4 strings for all existing rows (upper-case)
            migrationBuilder.Sql(@"
                UPDATE ""Cards"" SET ""NewId"" = upper(
                    hex(randomblob(4)) || '-' || hex(randomblob(2)) || '-4' ||
                    substr(hex(randomblob(2)), 2) || '-' ||
                    substr('89AB', abs(random()) % 4 + 1, 1) || substr(hex(randomblob(2)), 2) || '-' ||
                    hex(randomblob(6))
                )
            ");

            // Step 2: Rebuild Cards table with the new Guid PK
            migrationBuilder.Sql(@"
                CREATE TABLE ""Cards_new"" (
                    ""Id"" TEXT NOT NULL CONSTRAINT ""PK_Cards"" PRIMARY KEY,
                    ""Quantity"" INTEGER NOT NULL,
                    ""CardName"" TEXT NOT NULL,
                    ""NormalizedCardName"" TEXT,
                    ""Edition"" TEXT NOT NULL,
                    ""LanguageId"" TEXT,
                    ""CollectorNumber"" TEXT,
                    ""ScryfallId"" TEXT,
                    ""DeckId"" INTEGER,
                    ""ContainerId"" INTEGER,
                    ""Comments"" TEXT,
                    ""IsSideboard"" INTEGER NOT NULL,
                    ""IsFoil"" INTEGER NOT NULL,
                    ""Condition"" INTEGER,
                    ""IsLand"" INTEGER NOT NULL,
                    CONSTRAINT ""FK_Cards_Containers_ContainerId"" FOREIGN KEY (""ContainerId"") REFERENCES ""Containers"" (""Id""),
                    CONSTRAINT ""FK_Cards_Decks_DeckId"" FOREIGN KEY (""DeckId"") REFERENCES ""Decks"" (""Id""),
                    CONSTRAINT ""FK_Cards_CardLanguage_LanguageId"" FOREIGN KEY (""LanguageId"") REFERENCES ""CardLanguage"" (""Code""),
                    CONSTRAINT ""FK_Cards_ScryfallCardMetadata_ScryfallId"" FOREIGN KEY (""ScryfallId"") REFERENCES ""ScryfallCardMetadata"" (""Id"")
                )
            ");

            migrationBuilder.Sql(@"
                INSERT INTO ""Cards_new"" (""Id"", ""Quantity"", ""CardName"", ""NormalizedCardName"", ""Edition"", ""LanguageId"",
                    ""CollectorNumber"", ""ScryfallId"", ""DeckId"", ""ContainerId"", ""Comments"",
                    ""IsSideboard"", ""IsFoil"", ""Condition"", ""IsLand"")
                SELECT ""NewId"", ""Quantity"", ""CardName"", ""NormalizedCardName"", ""Edition"", ""LanguageId"",
                    ""CollectorNumber"", ""ScryfallId"", ""DeckId"", ""ContainerId"", ""Comments"",
                    ""IsSideboard"", ""IsFoil"", ""Condition"", ""IsLand""
                FROM ""Cards""
            ");

            // Step 3: Rebuild CardSkuTag table with updated FK referencing new GUIDs
            migrationBuilder.Sql(@"
                CREATE TABLE ""CardSkuTag_new"" (
                    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_CardSkuTag"" PRIMARY KEY AUTOINCREMENT,
                    ""CardSkuId"" TEXT NOT NULL,
                    ""Name"" TEXT NOT NULL,
                    CONSTRAINT ""FK_CardSkuTag_Cards_CardSkuId"" FOREIGN KEY (""CardSkuId"") REFERENCES ""Cards_new"" (""Id"") ON DELETE CASCADE
                )
            ");

            migrationBuilder.Sql(@"
                INSERT INTO ""CardSkuTag_new"" (""Id"", ""CardSkuId"", ""Name"")
                SELECT t.""Id"", c.""NewId"", t.""Name""
                FROM ""CardSkuTag"" t
                JOIN ""Cards"" c ON c.""Id"" = t.""CardSkuId""
            ");

            // Step 4: Drop old tables and rename new ones
            migrationBuilder.Sql(@"DROP TABLE ""CardSkuTag""");
            migrationBuilder.Sql(@"DROP TABLE ""Cards""");
            migrationBuilder.Sql(@"ALTER TABLE ""Cards_new"" RENAME TO ""Cards""");
            migrationBuilder.Sql(@"ALTER TABLE ""CardSkuTag_new"" RENAME TO ""CardSkuTag""");

            // Step 5: Recreate indexes
            migrationBuilder.Sql(@"CREATE INDEX ""IX_Cards_CardName"" ON ""Cards"" (""CardName"")");
            migrationBuilder.Sql(@"CREATE INDEX ""IX_Cards_ContainerId"" ON ""Cards"" (""ContainerId"")");
            migrationBuilder.Sql(@"CREATE INDEX ""IX_Cards_DeckId"" ON ""Cards"" (""DeckId"")");
            migrationBuilder.Sql(@"CREATE INDEX ""IX_Cards_LanguageId"" ON ""Cards"" (""LanguageId"")");
            migrationBuilder.Sql(@"CREATE INDEX ""IX_Cards_ScryfallId"" ON ""Cards"" (""ScryfallId"")");
            migrationBuilder.Sql(@"CREATE INDEX ""IX_CardSkuTag_CardSkuId"" ON ""CardSkuTag"" (""CardSkuId"")");
            migrationBuilder.Sql(@"CREATE INDEX ""IX_CardSkuTag_Name"" ON ""CardSkuTag"" (""Name"")");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // WARNING: Rolling back this migration will result in loss of all tag associations.
            // The original UUID→int ID mapping cannot be recovered, so CardSkuTag rows cannot be
            // reliably re-linked to the restored integer-keyed Cards rows.

            // Rebuild Cards table with new auto-increment integer PKs
            migrationBuilder.Sql(@"
                CREATE TABLE ""Cards_old"" (
                    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Cards"" PRIMARY KEY AUTOINCREMENT,
                    ""Quantity"" INTEGER NOT NULL,
                    ""CardName"" TEXT NOT NULL,
                    ""NormalizedCardName"" TEXT,
                    ""Edition"" TEXT NOT NULL,
                    ""LanguageId"" TEXT,
                    ""CollectorNumber"" TEXT,
                    ""ScryfallId"" TEXT,
                    ""DeckId"" INTEGER,
                    ""ContainerId"" INTEGER,
                    ""Comments"" TEXT,
                    ""IsSideboard"" INTEGER NOT NULL,
                    ""IsFoil"" INTEGER NOT NULL,
                    ""Condition"" INTEGER,
                    ""IsLand"" INTEGER NOT NULL,
                    CONSTRAINT ""FK_Cards_Containers_ContainerId"" FOREIGN KEY (""ContainerId"") REFERENCES ""Containers"" (""Id""),
                    CONSTRAINT ""FK_Cards_Decks_DeckId"" FOREIGN KEY (""DeckId"") REFERENCES ""Decks"" (""Id""),
                    CONSTRAINT ""FK_Cards_CardLanguage_LanguageId"" FOREIGN KEY (""LanguageId"") REFERENCES ""CardLanguage"" (""Code""),
                    CONSTRAINT ""FK_Cards_ScryfallCardMetadata_ScryfallId"" FOREIGN KEY (""ScryfallId"") REFERENCES ""ScryfallCardMetadata"" (""Id"")
                )
            ");

            migrationBuilder.Sql(@"
                INSERT INTO ""Cards_old"" (""Quantity"", ""CardName"", ""NormalizedCardName"", ""Edition"", ""LanguageId"",
                    ""CollectorNumber"", ""ScryfallId"", ""DeckId"", ""ContainerId"", ""Comments"",
                    ""IsSideboard"", ""IsFoil"", ""Condition"", ""IsLand"")
                SELECT ""Quantity"", ""CardName"", ""NormalizedCardName"", ""Edition"", ""LanguageId"",
                    ""CollectorNumber"", ""ScryfallId"", ""DeckId"", ""ContainerId"", ""Comments"",
                    ""IsSideboard"", ""IsFoil"", ""Condition"", ""IsLand""
                FROM ""Cards""
            ");

            // Rebuild empty CardSkuTag table with integer FK.
            // All existing tag associations are dropped because the UUID→int mapping is unavailable.
            migrationBuilder.Sql(@"
                CREATE TABLE ""CardSkuTag_old"" (
                    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_CardSkuTag"" PRIMARY KEY AUTOINCREMENT,
                    ""CardSkuId"" INTEGER NOT NULL,
                    ""Name"" TEXT NOT NULL,
                    CONSTRAINT ""FK_CardSkuTag_Cards_CardSkuId"" FOREIGN KEY (""CardSkuId"") REFERENCES ""Cards_old"" (""Id"") ON DELETE CASCADE
                )
            ");

            migrationBuilder.Sql(@"DROP TABLE ""CardSkuTag""");
            migrationBuilder.Sql(@"DROP TABLE ""Cards""");
            migrationBuilder.Sql(@"ALTER TABLE ""Cards_old"" RENAME TO ""Cards""");
            migrationBuilder.Sql(@"ALTER TABLE ""CardSkuTag_old"" RENAME TO ""CardSkuTag""");

            migrationBuilder.Sql(@"CREATE INDEX ""IX_Cards_CardName"" ON ""Cards"" (""CardName"")");
            migrationBuilder.Sql(@"CREATE INDEX ""IX_Cards_ContainerId"" ON ""Cards"" (""ContainerId"")");
            migrationBuilder.Sql(@"CREATE INDEX ""IX_Cards_DeckId"" ON ""Cards"" (""DeckId"")");
            migrationBuilder.Sql(@"CREATE INDEX ""IX_Cards_LanguageId"" ON ""Cards"" (""LanguageId"")");
            migrationBuilder.Sql(@"CREATE INDEX ""IX_Cards_ScryfallId"" ON ""Cards"" (""ScryfallId"")");
            migrationBuilder.Sql(@"CREATE INDEX ""IX_CardSkuTag_CardSkuId"" ON ""CardSkuTag"" (""CardSkuId"")");
            migrationBuilder.Sql(@"CREATE INDEX ""IX_CardSkuTag_Name"" ON ""CardSkuTag"" (""Name"")");
        }
    }
}
