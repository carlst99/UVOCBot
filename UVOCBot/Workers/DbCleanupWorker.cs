using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Core;
using UVOCBot.Core.Model;

namespace UVOCBot.Workers
{
    public class DbCleanupWorker : BackgroundService
    {
        private readonly IDbContextFactory<DiscordContext> _dbContextFactory;

        public DbCleanupWorker(IDbContextFactory<DiscordContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                using DiscordContext dbContext = _dbContextFactory.CreateDbContext();

                foreach (MemberGroup group in dbContext.MemberGroups)
                {
                    if (group.CreatedAt.AddHours(MemberGroup.MAX_LIFETIME_HOURS) < DateTimeOffset.UtcNow)
                        dbContext.MemberGroups.Remove(group);
                }

                await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

                await Task.Delay(900000, ct).ConfigureAwait(false); // Work every 15min
            }
        }
    }
}
