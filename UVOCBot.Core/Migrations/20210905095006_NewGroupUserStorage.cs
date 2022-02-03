using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace UVOCBot.Core.Migrations;

public partial class NewGroupUserStorage : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<byte[]>(
            name: "UserIds",
            table: "MemberGroups",
            type: "longblob",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "longtext")
            .OldAnnotation("MySql:CharSet", "utf8mb4");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "UserIds",
            table: "MemberGroups",
            type: "longtext",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "longblob")
            .Annotation("MySql:CharSet", "utf8mb4");
    }
}
