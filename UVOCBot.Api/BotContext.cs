using Microsoft.EntityFrameworkCore;
using System;
using UVOCBot.Api.Model;

namespace UVOCBot.Api
{
    public sealed class BotContext : DbContext
    {
        /*
         * Commands to setup migration variables
         * $env:UVOCBOT_DB_SERVER='localhost'
         * $env:UVOCBOT_DB_USER='uvocbot'
         * $env:UVOCBOT_DB_PASSWD=''
         * $env:UVOCBOT_DB_NAME='uvocbot_test'
         */

        private const string ENV_DB_SERVER = "UVOCBot.Api_DB_SERVER";
        private const string ENV_DB_USER = "UVOCBot.Api_DB_USER";
        private const string ENV_DB_PASSWD = "UVOCBot.Api_DB_PASSWD";
        private const string ENV_DB_NAME = "UVOCBot.Api_DB_NAME";

        public DbSet<GuildSettings> GuildSettings { get; set; }
        public DbSet<GuildTwitterSettings> GuildTwitterSettings { get; set; }
        public DbSet<TwitterUser> TwitterUsers { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            string dbServer = Environment.GetEnvironmentVariable(ENV_DB_SERVER);
            string dbUser = Environment.GetEnvironmentVariable(ENV_DB_USER);
            string dbPasswd = Environment.GetEnvironmentVariable(ENV_DB_PASSWD);
            string dbName = Environment.GetEnvironmentVariable(ENV_DB_NAME);
#if DEBUG
            string connectionString = "server = localhost; user = uvocbot_test; database = uvocbot_test";
#else
            string connectionString = $"server = {dbServer}; user = {dbUser}; password = {dbPasswd}; database = {dbName}";
#endif

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
