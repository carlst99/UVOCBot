using DbgCensus.EventStream.Abstractions;
using DbgCensus.EventStream.Commands;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UVOCBot.Plugins.Planetside.Services.Abstractions;

namespace UVOCBot.Plugins.Planetside.Workers
{
    public class SubscriptionWorker : BackgroundService
    {
        private readonly ILogger<SubscriptionWorker> _logger;
        private readonly IEventStreamClientFactory _eventStreamClientFactory;
        private readonly ISubscriptionBuilderService _subscriptionBuilder;

        public SubscriptionWorker(
            ILogger<SubscriptionWorker> logger,
            IEventStreamClientFactory eventStreamClientFactory,
            ISubscriptionBuilderService subscriptionBuilder)
        {
            _logger = logger;
            _eventStreamClientFactory = eventStreamClientFactory;
            _subscriptionBuilder = subscriptionBuilder;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    /* Resubscribe to our census events */
                    IEventStreamClient client = _eventStreamClientFactory.GetClient(EventStreamConstants.CENSUS_EVENTSTREAM_CLIENT_NAME);

                    if (client.IsRunning)
                    {
                        SubscribeCommand subscription = await _subscriptionBuilder.BuildAsync(ct).ConfigureAwait(false);
                        await client.SendCommandAsync(subscription, ct).ConfigureAwait(false);
                    }
                }
                catch (Exception ex) when (ex is not TaskCanceledException)
                {
                    _logger.LogError(ex, "Failed to refresh Census event stream subscription.");
                }

                await Task.Delay(TimeSpan.FromMinutes(15), ct).ConfigureAwait(false);
            }
        }
    }
}
