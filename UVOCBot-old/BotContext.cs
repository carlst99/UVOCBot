using Microsoft.EntityFrameworkCore;
using System.Linq;
using UVOCBot.Model;

namespace UVOCBot
{
    public sealed class BotContext : DbContext
    {
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

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            string dbPath = Program.GetAppdataFilePath("datastore.db");
            options.UseSqlite($"Data Source={dbPath}");
        }
    }
}
