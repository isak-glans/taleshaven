using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Taleshaven.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CharacterStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Conditions",
                table: "Characters",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Counters",
                table: "Characters",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SavedRolls",
                table: "Characters",
                type: "jsonb",
                nullable: true);

            // Befintliga karaktärer får tomma listor (B56).
            migrationBuilder.Sql("""UPDATE "Characters" SET "Counters" = '[]', "Conditions" = '[]', "SavedRolls" = '[]';""");
            migrationBuilder.Sql("""ALTER TABLE "Characters" ALTER COLUMN "Counters" SET DEFAULT '[]', ALTER COLUMN "Conditions" SET DEFAULT '[]', ALTER COLUMN "SavedRolls" SET DEFAULT '[]';""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Conditions",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Counters",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "SavedRolls",
                table: "Characters");
        }
    }
}
