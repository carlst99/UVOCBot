using DbgCensus.EventStream;
using DbgCensus.EventStream.EventHandlers.Extensions;
using DbgCensus.Rest;
using DbgCensus.Rest.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Remora.Commands.Extensions;
using UVOCBot.Plugins.Planetside;
using UVOCBot.Plugins.Planetside.CensusEventHandlers;
using UVOCBot.Plugins.Planetside.Commands;
using UVOCBot.Plugins.Planetside.Objects.EventStream;
using UVOCBot.Plugins.Planetside.Services;
using UVOCBot.Plugins.Planetside.Services.Abstractions;
using UVOCBot.Plugins.Planetside.Workers;

namespace UVOCBot.Plugins
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddPlanetsidePlugin(this IServiceCollection services, IConfiguration config)
        {
            PlanetsidePluginOptions pOptions = new();
            config.GetSection(nameof(PlanetsidePluginOptions)).Bind(pOptions);

            CensusQueryOptions queryOptions = new()
            {
                LanguageCode = "en",
                ServiceId = pOptions.CensusApiKey
            };
            EventStreamOptions esOptions = new()
            {
                ServiceId = pOptions.CensusApiKey
            };

            services.AddSingleton(Options.Create(pOptions));
            services.AddSingleton(Options.Create(queryOptions));
            services.AddSingleton(Options.Create(esOptions));

            services.AddHttpClient();
            services.AddSingleton<IFisuApiService, CachingFisuApiService>();

            services.AddCensusRestServices();
            services.AddSingleton<ICensusApiService, CachingCensusApiService>();

            services.AddCensusEventHandlingServices();
            services.AddSingleton<ISubscriptionBuilderService, SubscriptionBuilderService>();
            services.AddEventHandler<ConnectionStateChangedResponder>();
            services.AddEventHandler<FacilityControlResponder, FacilityControl>(EventStreamConstants.FACILITY_CONTROL_EVENT);
            services.AddEventHandler<MetagameEventResponder, MetagameEvent>(EventStreamConstants.METAGAME_EVENT_EVENT);

            services.AddCommandGroup<OtherCommands>();
            services.AddCommandGroup<OutfitTrackingCommands>();
            services.AddCommandGroup<WorldCommands>();

            services.AddHostedService<EventStreamWorker>();
            services.AddHostedService<StartupWorker>();
            services.AddHostedService<SubscriptionWorker>();

            return services;
        }
    }
}
