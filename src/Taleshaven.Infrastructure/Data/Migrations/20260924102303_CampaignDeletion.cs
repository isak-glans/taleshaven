using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Taleshaven.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CampaignDeletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Characters_CharacterId",
                table: "Posts");

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Characters_CharacterId",
                table: "Posts",
                column: "CharacterId",
                principalTable: "Characters",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Characters_CharacterId",
                table: "Posts");

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Characters_CharacterId",
                table: "Posts",
                column: "CharacterId",
                principalTable: "Characters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
