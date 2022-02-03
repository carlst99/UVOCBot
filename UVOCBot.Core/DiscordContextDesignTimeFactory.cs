using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;

namespace UVOCBot.Core;

public class DiscordContextDesignTimeFactory : IDesignTimeDbContextFactory<DiscordContext>
{
    public DiscordContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DiscordContext>();
        optionsBuilder.UseMySql("server = localhost; user = uvocbot_test; database = db_uvocbot_test", new MariaDbServerVersion(new Version("10.6.5")));

        return new DiscordContext(optionsBuilder.Options);
    }
}
