using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IO;
using Remora.Commands.Extensions;
using System;
using UVOCBot.Plugins.ApexLegends.Abstractions.Services;
using UVOCBot.Plugins.ApexLegends.Commands;
using UVOCBot.Plugins.ApexLegends.Objects;
using UVOCBot.Plugins.ApexLegends.Services;

// ReSharper disable once CheckNamespace
namespace UVOCBot.Plugins;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddApexLegendsPlugin(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<ApexPluginOptions>(config.GetSection(nameof(ApexPluginOptions)));

        services.AddHttpClient<IApexApiService, CachingApexApiService>((s, c) =>
        {
            ApexPluginOptions options = s.GetRequiredService<IOptions<ApexPluginOptions>>().Value;

            c.BaseAddress = new Uri(options.ApexLegendsApiEndpoint);
            c.DefaultRequestHeaders.Add("Authorization", options.ApexLegendsApiKey);
        });
        services.AddHttpClient<IApexImageGenerationService, ApexImageGenerationService>();

        services.AddSingleton<RecyclableMemoryStreamManager>();

        services.AddCommandTree()
                .WithCommandGroup<ApexCommands>()
                .Finish();

        return services;
    }
}
