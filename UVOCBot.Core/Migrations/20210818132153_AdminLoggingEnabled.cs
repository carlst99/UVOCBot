using Microsoft.EntityFrameworkCore.Migrations;

namespace UVOCBot.Core.Migrations;

public partial class AdminLoggingEnabled : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsLoggingEnabled",
            table: "GuildAdminSettings",
            type: "tinyint(1)",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "IsLoggingEnabled",
            table: "GuildAdminSettings");
    }
}
