using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Taleshaven.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UserProfilePortrait : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PortraitId",
                table: "AspNetUsers",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_PortraitId",
                table: "AspNetUsers",
                column: "PortraitId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Portraits_PortraitId",
                table: "AspNetUsers",
                column: "PortraitId",
                principalTable: "Portraits",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Portraits_PortraitId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_PortraitId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PortraitId",
                table: "AspNetUsers");
        }
    }
}
