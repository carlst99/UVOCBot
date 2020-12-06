using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using UVOCBot.Model;

namespace UVOCBot
{
    public sealed class BotContext : DbContext
    {
        private const string ENV_DB_SERVER = "UVOCBOT_DB_SERVER";
        private const string ENV_DB_USER = "UVOCBOT_DB_USER";
        private const string ENV_DB_PASSWD = "UVOCBOT_DB_PASSWD";
        private const string ENV_DB_NAME = "UVOCBOT_DB_NAME";

        public DbSet<BotSettings> BotSettings { get; set; }

        public BotSettings ActualBotSettings
        {
            get
            {
                if (BotSettings.Any())
                {
                    return BotSettings.First();
                }
                else
                {
                    BotSettings settings = UVOCBot.Model.BotSettings.Default;
                    BotSettings.Add(settings);
                    SaveChanges();
                    return settings;
                }
            }
        }

        public DbSet<GuildSettings> GuildSettings { get; set; }
        public DbSet<GuildTwitterSettings> GuildTwitterSettings { get; set; }
        public DbSet<TwitterUser> TwitterUsers { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            string dbServer = Environment.GetEnvironmentVariable(ENV_DB_SERVER);
            string dbUser = Environment.GetEnvironmentVariable(ENV_DB_USER);
            string dbPasswd = Environment.GetEnvironmentVariable(ENV_DB_PASSWD);
            string dbName = Environment.GetEnvironmentVariable(ENV_DB_NAME);
            string connectionString = $"server = {dbServer}; user = {dbUser}; password = {dbPasswd}; database = {dbName}";

            string dbPath = Program.GetAppdataFilePath("datastore.db");
            options.UseMySql(
                connectionString,
                new MariaDbServerVersion(new Version(10, 3, 27)),
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
