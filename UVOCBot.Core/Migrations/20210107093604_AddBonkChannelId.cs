using Microsoft.EntityFrameworkCore.Migrations;

namespace UVOCBot.Core.Migrations
{
    public partial class AddBonkChannelId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "BonkChannelId",
                table: "GuildSettings",
                type: "bigint unsigned",
                nullable: true,
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
