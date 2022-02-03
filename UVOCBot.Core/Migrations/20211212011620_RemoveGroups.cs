using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UVOCBot.Core.Migrations;

public partial class RemoveGroups : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "MemberGroups");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "MemberGroups",
            columns: table => new {
                Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                CreatorId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                GroupName = table.Column<string>(type: "varchar(255)", nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                GuildId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                UserIds = table.Column<byte[]>(type: "longblob", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MemberGroups", x => x.Id);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_MemberGroups_GroupName",
            table: "MemberGroups",
            column: "GroupName");
    }
}
