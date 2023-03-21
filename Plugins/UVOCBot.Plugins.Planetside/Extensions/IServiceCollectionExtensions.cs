using DbgCensus.EventStream;
using DbgCensus.EventStream.EventHandlers.Extensions;
using DbgCensus.Rest;
using DbgCensus.Rest.Extensions;
using DbgCensus.Rest.Objects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Remora.Commands.Extensions;
using Remora.Discord.Commands.Extensions;
using UVOCBot.Plugins.Planetside;
using UVOCBot.Plugins.Planetside.Abstractions.Services;
using UVOCBot.Plugins.Planetside.CensusEventHandlers;
using UVOCBot.Plugins.Planetside.Commands;
using UVOCBot.Plugins.Planetside.Services;
using UVOCBot.Plugins.Planetside.Workers;

// ReSharper disable once CheckNamespace
namespace UVOCBot.Plugins;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddPlanetsidePlugin(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<PlanetsidePluginOptions>(config.GetSection(nameof(PlanetsidePluginOptions)));
        services.Configure<EventStreamOptions>(config.GetSection(nameof(EventStreamOptions)));
        services.Configure<CensusQueryOptions>(config.GetSection(nameof(CensusQueryOptions)));
        services.Configure<CensusQueryOptions>(o => o.LanguageCode = CensusLanguage.English);
        services.Configure<CensusQueryOptions>("sanctuary", config.GetSection(nameof(CensusQueryOptions)));
        services.Configure<CensusQueryOptions>("sanctuary", o =>
        {
            o.LanguageCode = CensusLanguage.English;
            o.RootEndpoint = "https://census.lithafalcon.cc";
        });

        services.AddSingleton<HonuPopulationService>();
        services.AddSingleton<IPopulationService, SanctuaryPopulationService>();

        services.AddCensusRestServices();
        services.AddSingleton<ICensusApiService, CachingCensusApiService>();
        services.AddSingleton<IFacilityCaptureService, FacilityCaptureService>();

        services.AddCensusEventHandlingServices();
        services.AddPayloadHandler<ConnectionStateChangedResponder>();
        services.AddPayloadHandler<FacilityControlResponder>();
        services.AddPayloadHandler<MetagameEventResponder>();
        services.AddPayloadHandler<PlayerFacilityCaptureResponder>();

        services.AddCommandTree()
                .WithCommandGroup<CharacterCommands>()
                .WithCommandGroup<OtherCommands>()
                .WithCommandGroup<OutfitTrackingCommands>()
                .WithCommandGroup<OutfitWarCommands>()
                .WithCommandGroup<WorldCommands>()
                .Finish()
                .AddAutocompleteProvider<CharacterNameAutocompleteProvider>();

        services.AddHostedService<EventStreamWorker>();
        services.AddHostedService<CensusStateWorker>();

        return services;
    }
}
