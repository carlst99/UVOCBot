using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace UVOCBot.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BotSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TimeOfLastTwitterFetch = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuildSettings",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuildTwitterSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    RelayChannelId = table.Column<ulong>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildTwitterSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TwitterUser",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TwitterUser", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuildTwitterSettingsTwitterUser",
                columns: table => new
                {
                    GuildsId = table.Column<int>(type: "INTEGER", nullable: false),
                    TwitterUsersId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildTwitterSettingsTwitterUser", x => new { x.GuildsId, x.TwitterUsersId });
                    table.ForeignKey(
                        name: "FK_GuildTwitterSettingsTwitterUser_GuildTwitterSettings_GuildsId",
                        column: x => x.GuildsId,
                        principalTable: "GuildTwitterSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GuildTwitterSettingsTwitterUser_TwitterUser_TwitterUsersId",
                        column: x => x.TwitterUsersId,
                        principalTable: "TwitterUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuildTwitterSettingsTwitterUser_TwitterUsersId",
                table: "GuildTwitterSettingsTwitterUser",
                column: "TwitterUsersId");
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
                name: "TwitterUser");
        }
    }
}
