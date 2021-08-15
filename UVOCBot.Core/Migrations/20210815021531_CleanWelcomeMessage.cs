using Microsoft.EntityFrameworkCore.Migrations;

namespace UVOCBot.Core.Migrations
{
    public partial class CleanWelcomeMessage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SerialisedAlternateRoles",
                table: "GuildWelcomeMessages");

            migrationBuilder.DropColumn(
                name: "SerialisedDefaultRoles",
                table: "GuildWelcomeMessages");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SerialisedAlternateRoles",
                table: "GuildWelcomeMessages",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SerialisedDefaultRoles",
                table: "GuildWelcomeMessages",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
