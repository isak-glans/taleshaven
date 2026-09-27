using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Taleshaven.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ThreadModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Threads_CampaignId_Kind",
                table: "Threads");

            migrationBuilder.DropIndex(
                name: "IX_Threads_CampaignId_Ooc",
                table: "Threads");

            // Beskrivningen blir introduktionen (B25), med samma innehåll men större gräns.
            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Threads",
                newName: "Introduction");

            migrationBuilder.AlterColumn<string>(
                name: "Introduction",
                table: "Threads",
                type: "character varying(5000)",
                maxLength: 5000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

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


            migrationBuilder.AddColumn<int>(
                name: "Position",
                table: "Threads",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "Threads",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "Posts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedById",
                table: "Posts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ReplyToPostId",
                table: "Posts",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Rolls",
                table: "Posts",
                type: "jsonb",
                nullable: true);

            // Befintliga trådar: RPG först och OOC sedan (Kind 0 och 1), ändrade när de skapades.
            // Befintliga inlägg har inga slag i texten.
            migrationBuilder.Sql("""
                UPDATE "Threads" SET "Position" = "Kind", "UpdatedAt" = "CreatedAt";
                UPDATE "Posts" SET "Rolls" = '[]' WHERE "Rolls" IS NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Threads_CampaignId_Position",
                table: "Threads",
                columns: new[] { "CampaignId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_Threads_ChronicleEditedById",
                table: "Threads",
                column: "ChronicleEditedById");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_DeletedById",
                table: "Posts",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_ReplyToPostId",
                table: "Posts",
                column: "ReplyToPostId");

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_AspNetUsers_DeletedById",
                table: "Posts",
                column: "DeletedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Posts_ReplyToPostId",
                table: "Posts",
                column: "ReplyToPostId",
                principalTable: "Posts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Threads_AspNetUsers_ChronicleEditedById",
                table: "Threads",
                column: "ChronicleEditedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Posts_AspNetUsers_DeletedById",
                table: "Posts");

            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Posts_ReplyToPostId",
                table: "Posts");

            migrationBuilder.DropForeignKey(
                name: "FK_Threads_AspNetUsers_ChronicleEditedById",
                table: "Threads");

            migrationBuilder.DropIndex(
                name: "IX_Threads_CampaignId_Position",
                table: "Threads");

            migrationBuilder.DropIndex(
                name: "IX_Threads_ChronicleEditedById",
                table: "Threads");

            migrationBuilder.DropIndex(
                name: "IX_Posts_DeletedById",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_ReplyToPostId",
                table: "Posts");

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
                name: "Position",
                table: "Threads");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Threads");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "ReplyToPostId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "Rolls",
                table: "Posts");

            migrationBuilder.Sql("""UPDATE "Threads" SET "Introduction" = left("Introduction", 500);""");

            migrationBuilder.AlterColumn<string>(
                name: "Introduction",
                table: "Threads",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(5000)",
                oldMaxLength: 5000);

            migrationBuilder.RenameColumn(
                name: "Introduction",
                table: "Threads",
                newName: "Description");

            migrationBuilder.CreateIndex(
                name: "IX_Threads_CampaignId_Kind",
                table: "Threads",
                columns: new[] { "CampaignId", "Kind" });

            migrationBuilder.CreateIndex(
                name: "IX_Threads_CampaignId_Ooc",
                table: "Threads",
                column: "CampaignId",
                unique: true,
                filter: "\"Kind\" = 1");
        }
    }
}
