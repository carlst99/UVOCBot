using Microsoft.Extensions.DependencyInjection;
using Remora.Commands.Extensions;
using UVOCBot.Plugins.Feeds.Commands;

namespace UVOCBot.Plugins;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddFeedsPlugin(this IServiceCollection services)
    {
        services.AddCommandTree()
                .WithCommandGroup<FeedCommands>()
                .Finish();

        return services;
    }
}
