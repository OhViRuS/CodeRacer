using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeRacer.Server.Migrations;

/// <inheritdoc />
public partial class UniqueSnippetCodeText : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_CodeSnippets_CodeText",
            table: "CodeSnippets",
            column: "CodeText",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_CodeSnippets_CodeText",
            table: "CodeSnippets");
    }
}
