using DbgCensus.EventStream;
using DbgCensus.EventStream.EventHandlers.Extensions;
using DbgCensus.Rest;
using DbgCensus.Rest.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Remora.Commands.Extensions;
using UVOCBot.Plugins.Planetside;
using UVOCBot.Plugins.Planetside.Abstractions.Services;
using UVOCBot.Plugins.Planetside.CensusEventHandlers;
using UVOCBot.Plugins.Planetside.Commands;
using UVOCBot.Plugins.Planetside.Services;
using UVOCBot.Plugins.Planetside.Workers;

namespace UVOCBot.Plugins;

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
        services.AddSingleton<IPopulationService, FisuPopulationService>();

        services.AddCensusRestServices();
        services.AddSingleton<ICensusApiService, CachingCensusApiService>();

        services.AddCensusEventHandlingServices();
        services.AddSingleton<ISubscriptionBuilderService, SubscriptionBuilderService>();
        services.AddPayloadHandler<ConnectionStateChangedResponder>();
        services.AddPayloadHandler<FacilityControlResponder>();
        services.AddPayloadHandler<MetagameEventResponder>();

        services.AddCommandTree()
                .WithCommandGroup<OtherCommands>()
                .WithCommandGroup<OutfitTrackingCommands>()
                .WithCommandGroup<WorldCommands>()
                .Finish();

        services.AddHostedService<EventStreamWorker>();
        services.AddHostedService<StartupWorker>();

        return services;
    }
}
