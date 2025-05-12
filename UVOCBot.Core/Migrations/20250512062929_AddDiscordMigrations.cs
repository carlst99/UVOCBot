using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UVOCBot.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscordMigrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiscordMigrationHistory",
                columns: table => new
                {
                    MigrationId = table.Column<int>(type: "integer", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordMigrationHistory", x => x.MigrationId);
                });

            migrationBuilder.Sql("insert into \"DiscordMigrationHistory\" values (0, NOW())");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiscordMigrationHistory");
        }
    }
}
