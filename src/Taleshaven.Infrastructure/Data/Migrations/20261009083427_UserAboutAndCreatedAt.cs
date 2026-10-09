using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Taleshaven.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UserAboutAndCreatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "About",
                table: "AspNetUsers",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            // Befintliga konton får sin tidigaste aktivitet som skapandedatum (B52): kampanj, ansökan, medlemskap, inlägg eller porträtt.
            migrationBuilder.Sql("""
                UPDATE "AspNetUsers" u SET "CreatedAt" = (
                    SELECT min(t) FROM (
                        SELECT min(c."CreatedAt") AS t FROM "Campaigns" c WHERE c."GameMasterId" = u."Id"
                        UNION ALL SELECT min(a."SubmittedAt") FROM "CampaignApplications" a WHERE a."UserId" = u."Id"
                        UNION ALL SELECT min(m."JoinedAt") FROM "CampaignMemberships" m WHERE m."UserId" = u."Id"
                        UNION ALL SELECT min(p."CreatedAt") FROM "Posts" p WHERE p."AuthorId" = u."Id"
                        UNION ALL SELECT min(pt."CreatedAt") FROM "Portraits" pt WHERE pt."UploadedById" = u."Id"
                    ) times);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "About",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AspNetUsers");
        }
    }
}
