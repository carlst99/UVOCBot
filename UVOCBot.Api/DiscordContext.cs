using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using UVOCBot.Api.Model;

namespace UVOCBot.Api
{
    public sealed class DiscordContext : DbContext
    {
        private readonly DatabaseOptions _config;

        public DbSet<GuildSettings> GuildSettings { get; set; }
        public DbSet<GuildTwitterSettings> GuildTwitterSettings { get; set; }
        public DbSet<TwitterUser> TwitterUsers { get; set; }
        public DbSet<PlanetsideSettings> PlanetsideSettings { get; set; }
        public DbSet<MemberGroup> MemberGroups { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public DiscordContext(IOptions<DatabaseOptions> config)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            _config = config.Value;
        }

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
