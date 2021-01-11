using Microsoft.EntityFrameworkCore.Migrations;

namespace UVOCBot.Api.Migrations
{
    public partial class AddBonkChannelId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "BonkChannelId",
                table: "GuildSettings",
                type: "bigint unsigned",
                nullable: false,
                defaultValue: 0ul);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BonkChannelId",
                table: "GuildSettings");
        }
    }
}
