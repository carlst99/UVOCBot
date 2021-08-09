using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Core;
using UVOCBot.Core.Model;

namespace UVOCBot.Api.Workers
{
    public class CleanupWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public CleanupWorker(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (true)
            {
                using (IServiceScope scope = _serviceScopeFactory.CreateScope())
                {
                    DiscordContext db = scope.ServiceProvider.GetRequiredService<DiscordContext>();

                    foreach (MemberGroup group in db.MemberGroups)
                    {
                        if (group.CreatedAt.AddHours(MemberGroup.MAX_LIFETIME_HOURS) < DateTimeOffset.UtcNow)
                            db.MemberGroups.Remove(group);
                    }

                    await db.SaveChangesAsync(stoppingToken).ConfigureAwait(false);
                }

                await Task.Delay(900000, stoppingToken).ConfigureAwait(false); // Work every 15min
            }
        }
    }
}
