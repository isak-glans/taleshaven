using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Taleshaven.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ImageManifest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Portraits_ImportKey",
                table: "Portraits");

            migrationBuilder.DropColumn(
                name: "ImportKey",
                table: "Portraits");

            migrationBuilder.AlterColumn<string>(
                name: "UploadedById",
                table: "Portraits",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "ContentHash",
                table: "Portraits",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Portraits_ContentHash",
                table: "Portraits",
                column: "ContentHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Portraits_ContentHash",
                table: "Portraits");

            migrationBuilder.DropColumn(
                name: "ContentHash",
                table: "Portraits");

            migrationBuilder.AlterColumn<string>(
                name: "UploadedById",
                table: "Portraits",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImportKey",
                table: "Portraits",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Portraits_ImportKey",
                table: "Portraits",
                column: "ImportKey",
                unique: true);
        }
    }
}
