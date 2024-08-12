using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace UVOCBot.Core;

public class DiscordContextDesignTimeFactory : IDesignTimeDbContextFactory<DiscordContext>
{
    public DiscordContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<DiscordContext> optionsBuilder = new();
        optionsBuilder.UseNpgsql("Host=localhost;Database=db_uvocbot;Username=postgres;Password=admin");

        return new DiscordContext(optionsBuilder.Options);
    }
}
