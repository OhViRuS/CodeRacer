using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeRacer.Server.Migrations;

/// <summary>
/// Makes usernames and emails unique regardless of letter case ("Bob" and "bob" count as the same).
/// The application already checks this case-insensitively; these indexes make the database enforce it
/// too, so two registrations arriving at the same moment cannot both succeed.
/// EF Core cannot model expression indexes, so they are created with SQL and are not in the model snapshot.
/// </summary>
public partial class CaseInsensitiveUserUniqueness : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE UNIQUE INDEX \"IX_Users_UserName_Lower\" ON \"Users\" (lower(\"UserName\"));");
        migrationBuilder.Sql("CREATE UNIQUE INDEX \"IX_Users_Email_Lower\" ON \"Users\" (lower(\"Email\"));");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Users_Email_Lower\";");
        migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Users_UserName_Lower\";");
    }
}
