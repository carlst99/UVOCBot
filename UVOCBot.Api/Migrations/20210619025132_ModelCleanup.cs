using Microsoft.EntityFrameworkCore.Migrations;

namespace UVOCBot.Api.Migrations
{
    public partial class ModelCleanup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BonkChannelId",
                table: "GuildSettings");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "BonkChannelId",
                table: "GuildSettings",
                type: "bigint unsigned",
                nullable: true);
        }
    }
}
