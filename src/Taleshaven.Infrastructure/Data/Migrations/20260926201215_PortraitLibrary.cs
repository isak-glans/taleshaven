using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Taleshaven.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class PortraitLibrary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvatarKey",
                table: "Characters");

            migrationBuilder.AddColumn<int>(
                name: "PortraitId",
                table: "Characters",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Portraits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ImageKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Tags = table.Column<List<string>>(type: "text[]", nullable: false),
                    Source = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    UploadedById = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Portraits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Portraits_AspNetUsers_UploadedById",
                        column: x => x.UploadedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Characters_PortraitId",
                table: "Characters",
                column: "PortraitId");

            migrationBuilder.CreateIndex(
                name: "IX_Portraits_Tags",
                table: "Portraits",
                column: "Tags")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_Portraits_UploadedById",
                table: "Portraits",
                column: "UploadedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Characters_Portraits_PortraitId",
                table: "Characters",
                column: "PortraitId",
                principalTable: "Portraits",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Characters_Portraits_PortraitId",
                table: "Characters");

            migrationBuilder.DropTable(
                name: "Portraits");

            migrationBuilder.DropIndex(
                name: "IX_Characters_PortraitId",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "PortraitId",
                table: "Characters");

            migrationBuilder.AddColumn<string>(
                name: "AvatarKey",
                table: "Characters",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }
    }
}
