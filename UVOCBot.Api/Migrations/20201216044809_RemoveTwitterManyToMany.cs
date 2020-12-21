using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace UVOCBot.Api.Migrations
{
    public partial class RemoveTwitterManyToMany : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BotSettings");

            migrationBuilder.AddColumn<ulong>(
                name: "GuildTwitterSettingsGuildId",
                table: "TwitterUsers",
                type: "bigint unsigned",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TwitterUsers_GuildTwitterSettingsGuildId",
                table: "TwitterUsers",
                column: "GuildTwitterSettingsGuildId");

            migrationBuilder.AddForeignKey(
                name: "FK_TwitterUsers_GuildTwitterSettings_GuildTwitterSettingsGuildId",
                table: "TwitterUsers",
                column: "GuildTwitterSettingsGuildId",
                principalTable: "GuildTwitterSettings",
                principalColumn: "GuildId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("INSERT INTO TwitterUsers(GuildTwitterSettingsGuildId) SELECT GuildsGuildId FROM GuildTwitterSettingsTwitterUser");

            migrationBuilder.DropTable(
                name: "GuildTwitterSettingsTwitterUser");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TwitterUsers_GuildTwitterSettings_GuildTwitterSettingsGuildId",
                table: "TwitterUsers");

            migrationBuilder.DropIndex(
                name: "IX_TwitterUsers_GuildTwitterSettingsGuildId",
                table: "TwitterUsers");

            migrationBuilder.DropColumn(
                name: "GuildTwitterSettingsGuildId",
                table: "TwitterUsers");

            migrationBuilder.CreateTable(
                name: "BotSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TimeOfLastTwitterFetch = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuildTwitterSettingsTwitterUser",
                columns: table => new
                {
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
    }
}
