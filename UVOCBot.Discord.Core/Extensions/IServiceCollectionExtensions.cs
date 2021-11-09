using Microsoft.Extensions.DependencyInjection;
using Remora.Commands.Extensions;
using Remora.Discord.API.Abstractions.Gateway.Commands;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Extensions;
using UVOCBot.Discord.Core.Commands.Conditions;
using UVOCBot.Discord.Core.ExecutionEvents;
using UVOCBot.Discord.Core.Responders;
using UVOCBot.Discord.Core.Services;
using UVOCBot.Discord.Core.Services.Abstractions;

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

        services.AddCondition<RequireContextCondition>();
        services.AddCondition<RequireGuildPermissionCondition>();

        services.AddPostExecutionEvent<ErrorFeedbackPostExecutionEvent>();

        services.AddResponder<VoiceStateUpdateResponder>();

        return services;
    }
}
