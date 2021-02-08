using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using UVOCBot.Api.Model;

namespace UVOCBot.Api
{
    public sealed class BotContext : DbContext
    {
        private readonly DatabaseOptions _config;

        public DbSet<GuildSettings> GuildSettings { get; set; }
        public DbSet<GuildTwitterSettings> GuildTwitterSettings { get; set; }
        public DbSet<TwitterUser> TwitterUsers { get; set; }

        public BotContext(IOptions<DatabaseOptions> config)
        {
            _config = config.Value;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseMySql(
                _config.ConnectionString,
                new MariaDbServerVersion(new Version(_config.DatabaseVersion)),
                mySqlOptions => mySqlOptions
                    .CharSetBehavior(Pomelo.EntityFrameworkCore.MySql.Infrastructure.CharSetBehavior.NeverAppend))
#if DEBUG
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors();
#else
                ;
#endif
        }
    }
}
