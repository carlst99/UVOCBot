using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace UVOCBot.Core.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "BotSettings",
            columns: table => new {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                TimeOfLastTwitterFetch = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BotSettings", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "GuildSettings",
            columns: table => new {
                GuildId = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_GuildSettings", x => x.GuildId);
            });

        migrationBuilder.CreateTable(
            name: "GuildTwitterSettings",
            columns: table => new {
                GuildId = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                RelayChannelId = table.Column<ulong>(type: "bigint unsigned", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_GuildTwitterSettings", x => x.GuildId);
            });

        migrationBuilder.CreateTable(
            name: "TwitterUsers",
            columns: table => new {
                UserId = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TwitterUsers", x => x.UserId);
            });

        migrationBuilder.CreateTable(
            name: "GuildTwitterSettingsTwitterUser",
            columns: table => new {
                GuildsGuildId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                TwitterUsersUserId = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_GuildTwitterSettingsTwitterUser", x => new { x.GuildsGuildId, x.TwitterUsersUserId });
                table.ForeignKey(
                    name: "FK_GuildTwitterSettingsTwitterUser_GuildTwitterSettings_GuildsG~",
                    column: x => x.GuildsGuildId,
                    principalTable: "GuildTwitterSettings",
                    principalColumn: "GuildId",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_GuildTwitterSettingsTwitterUser_TwitterUsers_TwitterUsersUse~",
                    column: x => x.TwitterUsersUserId,
                    principalTable: "TwitterUsers",
                    principalColumn: "UserId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_GuildTwitterSettingsTwitterUser_TwitterUsersUserId",
            table: "GuildTwitterSettingsTwitterUser",
            column: "TwitterUsersUserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "BotSettings");

        migrationBuilder.DropTable(
            name: "GuildSettings");

        migrationBuilder.DropTable(
            name: "GuildTwitterSettingsTwitterUser");

        migrationBuilder.DropTable(
            name: "GuildTwitterSettings");

        migrationBuilder.DropTable(
            name: "TwitterUsers");
    }
}
