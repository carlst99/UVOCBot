using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using UVOCBot.Core.Model;

namespace UVOCBot.Core
{
    public sealed class DiscordContext : DbContext
    {
        private readonly DatabaseOptions _config;

        public DbSet<GuildSettings> GuildSettings { get; set; }
        public DbSet<GuildTwitterSettings> GuildTwitterSettings { get; set; }
        public DbSet<GuildWelcomeMessage> GuildWelcomeMessages { get; set; }
        public DbSet<TwitterUser> TwitterUsers { get; set; }
        public DbSet<PlanetsideSettings> PlanetsideSettings { get; set; }
        public DbSet<MemberGroup> MemberGroups { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        /// <summary>
        /// Janky hardcoded constructor for generating migrations.
        /// </summary>
        public DiscordContext()
        {
            _config = new DatabaseOptions
            {
                ConnectionString = "server = localhost; user = uvocbot_test; database = db_uvocbot_test",
                DatabaseVersion = "10.5.8"
            };
        }

        public DiscordContext(IOptions<DatabaseOptions> config)
        {
            _config = config.Value;
        }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseMySql(
                _config.ConnectionString,
                new MariaDbServerVersion(new Version(_config.DatabaseVersion)))
#if DEBUG
                    .EnableSensitiveDataLogging()
                    .EnableDetailedErrors();
#else
                ;
#endif
        }
    }
}
