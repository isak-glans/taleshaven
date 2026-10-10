using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Taleshaven.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Forum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "CampaignId",
                table: "Threads",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Threads",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                table: "Threads",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPinned",
                table: "Threads",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ForumCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForumCategories", x => x.Id);
                });

            // Forumets första kategori (B72); starttrådarna läggs in när appen startar (de behöver en administratör som författare).
            migrationBuilder.Sql("""
                INSERT INTO "ForumCategories" ("Name", "Description", "Position")
                VALUES ('General', 'News, questions and everything that isn''t part of a campaign.', 1);
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Threads_CategoryId_IsPinned",
                table: "Threads",
                columns: new[] { "CategoryId", "IsPinned" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Threads_CampaignOrCategory",
                table: "Threads",
                sql: "(\"CampaignId\" IS NULL) <> (\"CategoryId\" IS NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_Threads_ForumCategories_CategoryId",
                table: "Threads",
                column: "CategoryId",
                principalTable: "ForumCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Threads_ForumCategories_CategoryId",
                table: "Threads");

            migrationBuilder.DropTable(
                name: "ForumCategories");

            migrationBuilder.DropIndex(
                name: "IX_Threads_CategoryId_IsPinned",
                table: "Threads");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Threads_CampaignOrCategory",
                table: "Threads");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Threads");

            migrationBuilder.DropColumn(
                name: "IsLocked",
                table: "Threads");

            migrationBuilder.DropColumn(
                name: "IsPinned",
                table: "Threads");

            migrationBuilder.AlterColumn<int>(
                name: "CampaignId",
                table: "Threads",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
