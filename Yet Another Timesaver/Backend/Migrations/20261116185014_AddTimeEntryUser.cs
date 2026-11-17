using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Yet_Another_Timesaver.Backend.Migrations
{
    public partial class AddTimeEntryUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "TimeEntries",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "TimeEntries"
                SET "UserId" = (SELECT "UserId" FROM "Users" ORDER BY "UserId" LIMIT 1)
                WHERE "UserId" IS NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_UserId",
                table: "TimeEntries",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TimeEntries_Users_UserId",
                table: "TimeEntries",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TimeEntries_Users_UserId",
                table: "TimeEntries");

            migrationBuilder.DropIndex(
                name: "IX_TimeEntries_UserId",
                table: "TimeEntries");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "TimeEntries");
        }
    }
}
