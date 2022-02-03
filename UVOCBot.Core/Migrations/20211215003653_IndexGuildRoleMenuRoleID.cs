using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UVOCBot.Core.Migrations;

public partial class IndexGuildRoleMenuRoleID : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_GuildRoleMenuRole_RoleId",
            table: "GuildRoleMenuRole",
            column: "RoleId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_GuildRoleMenuRole_RoleId",
            table: "GuildRoleMenuRole");
    }
}
