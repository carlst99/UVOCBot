﻿using DbgCensus.EventStream;
using DbgCensus.EventStream.EventHandlers.Extensions;
using DbgCensus.Rest;
using DbgCensus.Rest.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Remora.Commands.Extensions;
using Remora.Plugins.Abstractions;
using Remora.Plugins.Abstractions.Attributes;
using UVOCBot.Plugins.Planetside;
using UVOCBot.Plugins.Planetside.CensusEventHandlers;
using UVOCBot.Plugins.Planetside.Commands;
using UVOCBot.Plugins.Planetside.Objects.EventStream;
using UVOCBot.Plugins.Planetside.Services;
using UVOCBot.Plugins.Planetside.Services.Abstractions;
using UVOCBot.Plugins.Planetside.Workers;

[assembly: RemoraPlugin(typeof(PlanetsidePlugin))]

namespace UVOCBot.Plugins.Planetside
{
    public class PlanetsidePlugin : PluginDescriptor
    {
        public override string Name => "PlanetsidePlugin";

        public override string Description => "Provides means to retrieve Planetside game data.";

        public override void ConfigureServices(IServiceCollection serviceCollection)
        {
            IServiceProvider provider = serviceCollection.BuildServiceProvider();
            IConfiguration config = provider.GetRequiredService<IConfiguration>();

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

            serviceCollection.AddSingleton(Options.Create(pOptions));
            serviceCollection.AddSingleton(Options.Create(queryOptions));
            serviceCollection.AddSingleton(Options.Create(esOptions));

            serviceCollection.AddHttpClient();
            serviceCollection.AddSingleton<IFisuApiService, CachingFisuApiService>();
            serviceCollection.AddSingleton<ICensusApiService, CachingCensusApiService>();

            serviceCollection.AddCensusRestServices();
            serviceCollection.AddSingleton<ICensusApiService, CensusApiService>();

            serviceCollection.AddCensusEventHandlingServices();
            serviceCollection.AddSingleton<ISubscriptionBuilderService, SubscriptionBuilderService>();
            serviceCollection.AddEventHandler<ConnectionStateChangedResponder>();
            serviceCollection.AddEventHandler<FacilityControlResponder, FacilityControl>(EventStreamConstants.FACILITY_CONTROL_EVENT);

            serviceCollection.AddCommandGroup<OtherCommands>();
            serviceCollection.AddCommandGroup<OutfitTrackingCommands>();
            serviceCollection.AddCommandGroup<WorldCommands>();

            serviceCollection.AddHostedService<EventStreamWorker>();
            serviceCollection.AddHostedService<SubscriptionWorker>();
        }
    }
}