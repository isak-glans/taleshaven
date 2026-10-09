using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Taleshaven.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SavedRollWithoutMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ingen ändring i tabellerna: sparade slag har inget läge längre (B66). Fältet Mode ligger kvar i befintlig
            // jsonb i Characters.SavedRolls men läses inte.

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
