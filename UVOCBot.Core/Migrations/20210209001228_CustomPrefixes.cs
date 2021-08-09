using Microsoft.EntityFrameworkCore.Migrations;

namespace UVOCBot.Core.Migrations
{
    public partial class CustomPrefixes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<ulong>(
                name: "BonkChannelId",
                table: "GuildSettings",
                type: "bigint unsigned",
                nullable: true,
                oldClrType: typeof(ulong),
                oldType: "bigint unsigned");

            migrationBuilder.AddColumn<string>(
                name: "Prefix",
                table: "GuildSettings",
                type: "longtext",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Prefix",
                table: "GuildSettings");

            migrationBuilder.AlterColumn<ulong>(
                name: "BonkChannelId",
                table: "GuildSettings",
                type: "bigint unsigned",
                nullable: false,
                defaultValue: 0ul,
                oldClrType: typeof(ulong),
                oldType: "bigint unsigned",
                oldNullable: true);
        }
    }
}
