using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Taleshaven.Infrastructure.Data.Migrations
{
    /// <summary>
    /// RPG blir en chatt med en kanal per kampanj (beslut B6). De tidigare RPG-trådarna var testdata
    /// och tas bort tillsammans med sina inlägg (beslut B11); varje kampanj får en ny RPG-kanal.
    /// </summary>
    public partial class RpgChannel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "Threads" WHERE "Kind" = 0;

                INSERT INTO "Threads" ("CampaignId", "Kind", "Title", "Description", "Status", "CreatedById", "CreatedAt")
                SELECT "Id", 0, 'RPG', '', 0, "GameMasterId", "CreatedAt" FROM "Campaigns";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // De borttagna trådarna kan inte återskapas. RPG-kanalerna lämnas kvar.
        }
    }
}