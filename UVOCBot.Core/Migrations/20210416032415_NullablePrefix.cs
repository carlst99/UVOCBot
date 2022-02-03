using Microsoft.EntityFrameworkCore.Migrations;

namespace UVOCBot.Core.Migrations;

public partial class NullablePrefix : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "UserIds",
            table: "MemberGroups",
            type: "longtext",
            nullable: false,
            defaultValue: "",
            oldClrType: typeof(string),
            oldType: "longtext",
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "GroupName",
            table: "MemberGroups",
            type: "varchar(255)",
            nullable: false,
            defaultValue: "",
            oldClrType: typeof(string),
            oldType: "varchar(255)",
            oldNullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "UserIds",
            table: "MemberGroups",
            type: "longtext",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "longtext");

        migrationBuilder.AlterColumn<string>(
            name: "GroupName",
            table: "MemberGroups",
            type: "varchar(255)",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "varchar(255)");
    }
}
