using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace UVOCBot.Core.Migrations
{
    public partial class ImproveWelcomeMessage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "AlternateRoles",
                table: "GuildWelcomeMessages",
                type: "longblob",
                nullable: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "DefaultRoles",
                table: "GuildWelcomeMessages",
                type: "longblob",
                nullable: false);

            migrationBuilder.AddColumn<bool>(
                name: "OfferAlternateRoles",
                table: "GuildWelcomeMessages",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AlternateRoles",
                table: "GuildWelcomeMessages");

            migrationBuilder.DropColumn(
                name: "DefaultRoles",
                table: "GuildWelcomeMessages");

            migrationBuilder.DropColumn(
                name: "OfferAlternateRoles",
                table: "GuildWelcomeMessages");
        }
    }
}
