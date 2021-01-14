using Microsoft.EntityFrameworkCore;
using System;
using UVOCBot.Api.Model;

namespace UVOCBot.Api
{
    public sealed class BotContext : DbContext
    {
        private const string ENV_DB_SERVER = "UVOCBOTAPI_DB_SERVER";
        private const string ENV_DB_USER = "UVOCBOTAPI_DB_USER";
        private const string ENV_DB_PASSWD = "UVOCBOTAPI_DB_PASSWD";
        private const string ENV_DB_NAME = "UVOCBOTAPI_DB_NAME";
        private const string ENV_DB_VERSION = "UVOCBOTAPI_DB_VERSION";

        public DbSet<GuildSettings> GuildSettings { get; set; }
        public DbSet<GuildTwitterSettings> GuildTwitterSettings { get; set; }
        public DbSet<TwitterUser> TwitterUsers { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            string dbServer = Environment.GetEnvironmentVariable(ENV_DB_SERVER);
            string dbUser = Environment.GetEnvironmentVariable(ENV_DB_USER);
            string dbPasswd = Environment.GetEnvironmentVariable(ENV_DB_PASSWD);
            string dbName = Environment.GetEnvironmentVariable(ENV_DB_NAME);
            string dbVersion = Environment.GetEnvironmentVariable(ENV_DB_VERSION);

#if DEBUG
            const string connectionString = "server = localhost; user = uvocbot_test; database = uvocbot_test";
#else
            string connectionString = $"server = {dbServer}; user = {dbUser}; password = {dbPasswd}; database = {dbName}";
            Console.WriteLine(connectionString);
#endif
            options.UseMySql(
                connectionString,
                new MariaDbServerVersion(new Version(dbVersion)),
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
