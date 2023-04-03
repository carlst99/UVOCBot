using Microsoft.Extensions.DependencyInjection;
using Remora.Commands.Extensions;
using UVOCBot.Plugins.SpaceEngineers.Abstractions.Services;
using UVOCBot.Plugins.SpaceEngineers.Commands;
using UVOCBot.Plugins.SpaceEngineers.Services;

// ReSharper disable once CheckNamespace
namespace UVOCBot.Plugins;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddSpaceEngineersPlugin(this IServiceCollection services)
    {
        services.AddHttpClient<IVRageRemoteApi, VRageRemoteApi>();

        services.AddCommandTree()
            .WithCommandGroup<SpaceEngineersCommands>()
            .Finish();

        return services;
    }
}
