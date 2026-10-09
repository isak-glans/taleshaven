using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Taleshaven.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ImageKinds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImportKey",
                table: "Portraits",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "Portraits",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Taggen "icon" blir typen Icon (B61). Taggarna "icon" och "profile" styr inte längre något och tas bort;
            // alla bilder med dem har andra taggar kvar.
            migrationBuilder.Sql("""
                UPDATE "Portraits" SET "Kind" = 1 WHERE 'icon' = ANY("Tags");
                UPDATE "Portraits" SET "Tags" = array_remove(array_remove("Tags", 'icon'), 'profile')
                WHERE "Tags" && ARRAY['icon', 'profile']::text[];
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Portraits_ImportKey",
                table: "Portraits",
                column: "ImportKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Ikonerna får tillbaka taggen "icon"; "profile" går inte att återställa.
            migrationBuilder.Sql("""UPDATE "Portraits" SET "Tags" = array_append("Tags", 'icon') WHERE "Kind" = 1;""");

            migrationBuilder.DropIndex(
                name: "IX_Portraits_ImportKey",
                table: "Portraits");

            migrationBuilder.DropColumn(
                name: "ImportKey",
                table: "Portraits");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "Portraits");
        }
    }
}
