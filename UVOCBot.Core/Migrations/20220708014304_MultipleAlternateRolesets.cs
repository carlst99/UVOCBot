using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UVOCBot.Core.Migrations
{
    public partial class MultipleAlternateRolesets : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuildTwitterSettingsTwitterUser");

            migrationBuilder.DropTable(
                name: "GuildTwitterSettings");

            migrationBuilder.DropTable(
                name: "TwitterUsers");

            migrationBuilder.DropColumn(
                name: "AlternateRoles",
                table: "GuildWelcomeMessages");

            migrationBuilder.DropColumn(
                name: "OfferAlternateRoles",
                table: "GuildWelcomeMessages");
            
            migrationBuilder.DropColumn(
                name: "AlternateRoleLabel",
                table: "GuildWelcomeMessages");

            migrationBuilder.AddColumn<string>(
                name: "AlternateRolesets",
                table: "GuildWelcomeMessages",
                type: "longtext",
                nullable: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AlternateRolesets",
                table: "GuildWelcomeMessages");

            migrationBuilder.AddColumn<string>(
                name: "AlternateRoleLabel",
                table: "GuildWelcomeMessages",
                type: "longtext",
                nullable: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "AlternateRoles",
                table: "GuildWelcomeMessages",
                type: "longblob",
                nullable: false);

            migrationBuilder.AddColumn<bool>(
                name: "OfferAlternateRoles",
                table: "GuildWelcomeMessages",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "GuildTwitterSettings",
                columns: table => new
                {
                    GuildId = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RelayChannelId = table.Column<ulong>(type: "bigint unsigned", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildTwitterSettings", x => x.GuildId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TwitterUsers",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    LastRelayedTweetId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TwitterUsers", x => x.UserId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_GuildTwitterSettingsTwitterUser_TwitterUsersUserId",
                table: "GuildTwitterSettingsTwitterUser",
                column: "TwitterUsersUserId");
        }
    }
}
