using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UVOCBot.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddSEStatusMessageChannelId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "StatusMessageChannelId",
                table: "SpaceEngineersDatas",
                type: "bigint unsigned",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StatusMessageChannelId",
                table: "SpaceEngineersDatas");
        }
    }
}
