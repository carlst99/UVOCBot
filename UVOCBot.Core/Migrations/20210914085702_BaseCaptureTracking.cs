using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace UVOCBot.Core.Migrations
{
    public partial class BaseCaptureTracking : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "BaseCaptureChannelId",
                table: "PlanetsideSettings",
                type: "bigint unsigned",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "TrackedOutfits",
                table: "PlanetsideSettings",
                type: "longblob",
                nullable: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaseCaptureChannelId",
                table: "PlanetsideSettings");

            migrationBuilder.DropColumn(
                name: "TrackedOutfits",
                table: "PlanetsideSettings");
        }
    }
}
