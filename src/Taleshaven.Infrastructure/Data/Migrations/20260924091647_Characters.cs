using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Taleshaven.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Characters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CharacterId",
                table: "Posts",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Characters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CampaignId = table.Column<int>(type: "integer", nullable: false),
                    OwnerId = table.Column<string>(type: "text", nullable: false),
                    IsNpc = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Sheet = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    SheetUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RuleSystem = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    AvatarKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Characters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Characters_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Characters_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Posts_CharacterId",
                table: "Posts",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_CampaignId_OwnerId",
                table: "Characters",
                columns: new[] { "CampaignId", "OwnerId" });

            migrationBuilder.CreateIndex(
                name: "IX_Characters_OwnerId",
                table: "Characters",
                column: "OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Characters_CharacterId",
                table: "Posts",
                column: "CharacterId",
                principalTable: "Characters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Characters_CharacterId",
                table: "Posts");

            migrationBuilder.DropTable(
                name: "Characters");

            migrationBuilder.DropIndex(
                name: "IX_Posts_CharacterId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "CharacterId",
                table: "Posts");
        }
    }
}
