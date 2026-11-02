using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeRacer.Server.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CodeSnippets",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CodeText = table.Column<string>(type: "text", nullable: false),
                Language = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Difficulty = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                ProgrammingConcept = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CodeSnippets", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                PasswordHash = table.Column<string>(type: "text", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "RaceResults",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                CodeSnippetId = table.Column<Guid>(type: "uuid", nullable: false),
                CompletionTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                TypingSpeed = table.Column<double>(type: "double precision", nullable: false),
                Accuracy = table.Column<double>(type: "double precision", nullable: false),
                Mistakes = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RaceResults", x => x.Id);
                table.ForeignKey(
                    name: "FK_RaceResults_CodeSnippets_CodeSnippetId",
                    column: x => x.CodeSnippetId,
                    principalTable: "CodeSnippets",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_RaceResults_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_RaceResults_CodeSnippetId",
            table: "RaceResults",
            column: "CodeSnippetId");

        migrationBuilder.CreateIndex(
            name: "IX_RaceResults_UserId_CreatedAt",
            table: "RaceResults",
            columns: new[] { "UserId", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_Users_Email",
            table: "Users",
            column: "Email",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Users_UserName",
            table: "Users",
            column: "UserName",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "RaceResults");

        migrationBuilder.DropTable(
            name: "CodeSnippets");

        migrationBuilder.DropTable(
            name: "Users");
    }
}
