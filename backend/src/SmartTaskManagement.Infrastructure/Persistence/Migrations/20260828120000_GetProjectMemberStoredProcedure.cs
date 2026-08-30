using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTaskManagement.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class GetProjectMemberStoredProcedure : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE OR ALTER PROCEDURE [dbo].[getProjectMember]
                @project_id uniqueidentifier,
                @user_id uniqueidentifier
            AS
            BEGIN
                SET NOCOUNT ON;

                SELECT
                    [ProjectId],
                    [UserId],
                    [AddedByUserId],
                    [AddedAtUtc]
                FROM [dbo].[ProjectMembers]
                WHERE [ProjectId] = @project_id
                  AND [UserId] = @user_id;
            END;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP PROCEDURE IF EXISTS [dbo].[getProjectMember];
            """);
    }
}
