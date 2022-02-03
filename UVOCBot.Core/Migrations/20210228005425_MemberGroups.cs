using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace UVOCBot.Core.Migrations;

public partial class MemberGroups : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "MemberGroups",
            columns: table => new {
                Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                GuildId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                GroupName = table.Column<string>(type: "varchar(255)", nullable: true),
                UserIds = table.Column<string>(type: "longtext", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MemberGroups", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_MemberGroups_GroupName",
            table: "MemberGroups",
            column: "GroupName");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "MemberGroups");
    }
}
