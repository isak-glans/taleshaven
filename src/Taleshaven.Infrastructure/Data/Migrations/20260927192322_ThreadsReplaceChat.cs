using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Taleshaven.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ThreadsReplaceChat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChronicleChapters");

            migrationBuilder.DropColumn(
                name: "Roll",
                table: "Posts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Roll",
                table: "Posts",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ChronicleChapters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AuthorId = table.Column<string>(type: "text", nullable: false),
                    CampaignId = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChronicleChapters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChronicleChapters_AspNetUsers_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChronicleChapters_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChronicleChapters_AuthorId",
                table: "ChronicleChapters",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_ChronicleChapters_CampaignId_Position",
                table: "ChronicleChapters",
                columns: new[] { "CampaignId", "Position" });
        }
    }
}
