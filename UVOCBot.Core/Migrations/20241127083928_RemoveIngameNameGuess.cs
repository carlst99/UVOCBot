using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UVOCBot.Core.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIngameNameGuess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DoIngameNameGuess",
                table: "GuildWelcomeMessages");

            migrationBuilder.DropColumn(
                name: "OutfitId",
                table: "GuildWelcomeMessages");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DoIngameNameGuess",
                table: "GuildWelcomeMessages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "OutfitId",
                table: "GuildWelcomeMessages",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
