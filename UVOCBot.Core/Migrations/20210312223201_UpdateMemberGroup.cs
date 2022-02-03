using Microsoft.EntityFrameworkCore.Migrations;

namespace UVOCBot.Core.Migrations;

public partial class UpdateMemberGroup : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<ulong>(
            name: "CreatorId",
            table: "MemberGroups",
            type: "bigint unsigned",
            nullable: false,
            defaultValue: 0ul);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CreatorId",
            table: "MemberGroups");
    }
}
