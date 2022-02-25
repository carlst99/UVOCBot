using Microsoft.Extensions.DependencyInjection;
using Remora.Commands.Extensions;
using Remora.Discord.API.Abstractions.Gateway.Commands;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Extensions;
using UVOCBot.Discord.Core.Abstractions.Services;
using UVOCBot.Discord.Core.Commands.Conditions;
using UVOCBot.Discord.Core.Components;
using UVOCBot.Discord.Core.ExecutionEvents;
using UVOCBot.Discord.Core.Responders;
using UVOCBot.Discord.Core.Services;

namespace UVOCBot.Discord.Core.Extensions;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddCoreDiscordServices(this IServiceCollection services)
    {
        services.Configure<DiscordGatewayClientOptions>
        (
            o => o.Intents |= GatewayIntents.GuildVoiceStates
        );

        services.AddSingleton<IPermissionChecksService, PermissionChecksService>();
        services.AddSingleton<IVoiceStateCacheService, VoiceStateCacheService>();

        services.AddCondition<RequireGuildPermissionCondition>();

        services.AddPostExecutionEvent<ErrorFeedbackPostExecutionEvent>();

        services.AddResponder<ComponentInteractionResponder>();
        services.AddResponder<VoiceStateUpdateResponder>();

        return services;
    }

    /// <summary>
    /// Adds a component responder to the service collection.
    /// </summary>
    /// <typeparam name="TResponder">The type of the responder.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="key">The key under which to register the responders.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddComponentResponder<TResponder>(this IServiceCollection services, string key) where TResponder : class, IComponentResponder
    {
        services.Configure<ComponentResponderRepository>(r => r.AddResponder<TResponder>(key));
        services.AddScoped<TResponder>();

        return services;
    }
}
