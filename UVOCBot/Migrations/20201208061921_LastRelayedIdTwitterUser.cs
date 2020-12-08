using Microsoft.EntityFrameworkCore.Migrations;

namespace UVOCBot.Migrations
{
    public partial class LastRelayedIdTwitterUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "LastRelayedTweetId",
                table: "TwitterUsers",
                type: "bigint",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastRelayedTweetId",
                table: "TwitterUsers");
        }
    }
}
