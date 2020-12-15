using Microsoft.EntityFrameworkCore;
using System;
using UVOCBotApi.Model;

namespace UVOCBotApi
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

        private const string ENV_DB_SERVER = "UVOCBOTAPI_DB_SERVER";
        private const string ENV_DB_USER = "UVOCBOTAPI_DB_USER";
        private const string ENV_DB_PASSWD = "UVOCBOTAPI_DB_PASSWD";
        private const string ENV_DB_NAME = "UVOCBOTAPI_DB_NAME";

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
