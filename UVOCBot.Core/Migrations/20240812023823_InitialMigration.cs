using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UVOCBot.Core.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuildAdminSettings",
                columns: table => new
                {
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    IsLoggingEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LoggingChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    LogTypes = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildAdminSettings", x => x.GuildId);
                });

            migrationBuilder.CreateTable(
                name: "GuildFeedsSettings",
                columns: table => new
                {
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    FeedChannelID = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    Feeds = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildFeedsSettings", x => x.GuildId);
                });

            migrationBuilder.CreateTable(
                name: "GuildWelcomeMessages",
                columns: table => new
                {
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    AlternateRolesets = table.Column<string>(type: "text", nullable: false),
                    ChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    DefaultRoles = table.Column<byte[]>(type: "bytea", nullable: false),
                    DoIngameNameGuess = table.Column<bool>(type: "boolean", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    OutfitId = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildWelcomeMessages", x => x.GuildId);
                });

            migrationBuilder.CreateTable(
                name: "PlanetsideSettings",
                columns: table => new
                {
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    DefaultWorld = table.Column<int>(type: "integer", nullable: true),
                    TrackedOutfits = table.Column<byte[]>(type: "bytea", nullable: false),
                    BaseCaptureChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanetsideSettings", x => x.GuildId);
                });

            migrationBuilder.CreateTable(
                name: "RoleMenus",
                columns: table => new
                {
                    Id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    AuthorId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    ChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    MessageId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleMenus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SpaceEngineersDatas",
                columns: table => new
                {
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    ServerAddress = table.Column<string>(type: "text", nullable: true),
                    ServerPort = table.Column<int>(type: "integer", nullable: true),
                    ServerKey = table.Column<string>(type: "text", nullable: true),
                    StatusMessageChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    StatusMessageId = table.Column<decimal>(type: "numeric(20,0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpaceEngineersDatas", x => x.GuildId);
                });

            migrationBuilder.CreateTable(
                name: "GuildRoleMenuRole",
                columns: table => new
                {
                    Id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    RoleId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Emoji = table.Column<string>(type: "text", nullable: true),
                    GuildRoleMenuId = table.Column<decimal>(type: "numeric(20,0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildRoleMenuRole", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuildRoleMenuRole_RoleMenus_GuildRoleMenuId",
                        column: x => x.GuildRoleMenuId,
                        principalTable: "RoleMenus",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuildRoleMenuRole_GuildRoleMenuId",
                table: "GuildRoleMenuRole",
                column: "GuildRoleMenuId");

            migrationBuilder.CreateIndex(
                name: "IX_GuildRoleMenuRole_RoleId",
                table: "GuildRoleMenuRole",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleMenus_GuildId_MessageId",
                table: "RoleMenus",
                columns: new[] { "GuildId", "MessageId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuildAdminSettings");

            migrationBuilder.DropTable(
                name: "GuildFeedsSettings");

            migrationBuilder.DropTable(
                name: "GuildRoleMenuRole");

            migrationBuilder.DropTable(
                name: "GuildWelcomeMessages");

            migrationBuilder.DropTable(
                name: "PlanetsideSettings");

            migrationBuilder.DropTable(
                name: "SpaceEngineersDatas");

            migrationBuilder.DropTable(
                name: "RoleMenus");
        }
    }
}
