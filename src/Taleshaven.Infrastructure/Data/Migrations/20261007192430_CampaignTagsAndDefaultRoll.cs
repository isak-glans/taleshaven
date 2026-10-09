using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Taleshaven.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CampaignTagsAndDefaultRoll : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultRoll",
                table: "Campaigns",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "1d20");

            migrationBuilder.AddColumn<List<string>>(
                name: "Tags",
                table: "Campaigns",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'::text[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultRoll",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Campaigns");
        }
    }
}
