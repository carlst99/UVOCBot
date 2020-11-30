using Microsoft.EntityFrameworkCore.Migrations;

namespace UVOCBot.Migrations
{
    public partial class RemoveGuildLinkFromTwitterSettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GuildTwitterSettings_GuildSettings_GuildId",
                table: "GuildTwitterSettings");

            migrationBuilder.DropIndex(
                name: "IX_GuildTwitterSettings_GuildId",
                table: "GuildTwitterSettings");

            migrationBuilder.AlterColumn<ulong>(
                name: "GuildId",
                table: "GuildTwitterSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0ul,
                oldClrType: typeof(ulong),
                oldType: "INTEGER",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<ulong>(
                name: "GuildId",
                table: "GuildTwitterSettings",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(ulong),
                oldType: "INTEGER");

            migrationBuilder.CreateIndex(
                name: "IX_GuildTwitterSettings_GuildId",
                table: "GuildTwitterSettings",
                column: "GuildId");

            migrationBuilder.AddForeignKey(
                name: "FK_GuildTwitterSettings_GuildSettings_GuildId",
                table: "GuildTwitterSettings",
                column: "GuildId",
                principalTable: "GuildSettings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
