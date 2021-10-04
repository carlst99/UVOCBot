using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Remora.Results;
using UVOCBot.Plugins.Planetside.Objects;
using UVOCBot.Plugins.Planetside.Objects.CensusQuery;
using UVOCBot.Plugins.Planetside.Objects.EventStream;
using UVOCBot.Plugins.Planetside.Services.Abstractions;

namespace UVOCBot.Plugins.Planetside.Workers
{
    public class StartupWorker : BackgroundService
    {
        private readonly ICensusApiService _censusApi;
        private readonly IMemoryCache _cache;

        public StartupWorker
        (
            ICensusApiService censusApi,
            IMemoryCache cache
        )
        {
            _censusApi = censusApi;
            _cache = cache;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            // Pre-cache metagame events.
            foreach (ValidWorldDefinition world in Enum.GetValues<ValidWorldDefinition>())
            {
                Result<List<QueryMetagameEvent>> events = await _censusApi.GetMetagameEventsAsync(world, ct: ct).ConfigureAwait(false);
                if (!events.IsDefined())
                    continue;

                if (events.Entity.Count == 0)
                    continue;

                // Assume events are ordered by timestamp, as we do this in the query
                MetagameEvent eventStreamConversion = events.Entity[0].ToEventStreamMetagameEvent();
                _cache.Set(eventStreamConversion.GetCacheKey(), eventStreamConversion);
            }

            // TODO: Populate mapping data from Census query
            // Will need to convert to event stream objects
        }
    }
}
