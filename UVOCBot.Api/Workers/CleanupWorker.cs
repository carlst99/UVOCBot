using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Api.Model;

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
                    BotContext db = scope.ServiceProvider.GetRequiredService<BotContext>();

                    foreach (MemberGroup group in db.MemberGroups)
                    {
                        if (group.CreatedAt.AddHours(MemberGroup.MAX_LIFETIME_HOURS) > DateTimeOffset.Now)
                            db.MemberGroups.Remove(group);
                    }

                    await db.SaveChangesAsync().ConfigureAwait(false);
                }

                await Task.Delay(900000, stoppingToken).ConfigureAwait(false); // Work every 15min
            }
        }
    }
}
