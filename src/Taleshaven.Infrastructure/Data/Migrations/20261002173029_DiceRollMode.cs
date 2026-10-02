using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Taleshaven.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class DiceRollMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Slagen ligger som jsonb i Posts.Rolls; äldre slag får läget Normal (B42).
            migrationBuilder.Sql("""
                UPDATE "Posts"
                SET "Rolls" = (SELECT jsonb_agg(roll || '{"Mode": 0}'::jsonb) FROM jsonb_array_elements("Rolls") AS roll)
                WHERE jsonb_array_length("Rolls") > 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
