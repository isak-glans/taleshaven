using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Taleshaven.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveThreadKindAndChronicle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Threads_AspNetUsers_ChronicleEditedById",
                table: "Threads");

            migrationBuilder.DropIndex(
                name: "IX_Threads_ChronicleEditedById",
                table: "Threads");

            migrationBuilder.DropColumn(
                name: "Chronicle",
                table: "Threads");

            migrationBuilder.DropColumn(
                name: "ChronicleEditedAt",
                table: "Threads");

            migrationBuilder.DropColumn(
                name: "ChronicleEditedById",
                table: "Threads");

            migrationBuilder.DropColumn(
                name: "Introduction",
                table: "Threads");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "Threads");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Chronicle",
                table: "Threads",
                type: "character varying(5000)",
                maxLength: 5000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ChronicleEditedAt",
                table: "Threads",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChronicleEditedById",
                table: "Threads",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Introduction",
                table: "Threads",
                type: "character varying(5000)",
                maxLength: 5000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "Threads",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Threads_ChronicleEditedById",
                table: "Threads",
                column: "ChronicleEditedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Threads_AspNetUsers_ChronicleEditedById",
                table: "Threads",
                column: "ChronicleEditedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
