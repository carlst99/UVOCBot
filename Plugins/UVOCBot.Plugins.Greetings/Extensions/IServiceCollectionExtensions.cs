using DbgCensus.Rest.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Remora.Commands.Extensions;
using Remora.Discord.Gateway.Extensions;
using UVOCBot.Discord.Core.Extensions;
using UVOCBot.Plugins.Greetings;
using UVOCBot.Plugins.Greetings.Abstractions.Services;
using UVOCBot.Plugins.Greetings.Commands;
using UVOCBot.Plugins.Greetings.Responders;
using UVOCBot.Plugins.Greetings.Services;

namespace UVOCBot.Plugins;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddGreetingsPlugin(this IServiceCollection services)
    {
        services.AddCensusRestServices();
        services.AddSingleton<ICensusQueryService, CensusQueryService>();

        services.AddScoped<IGreetingService, GreetingService>();
        services.AddResponder<GuildMemberAddGreetingResponder>();
        services.AddComponentResponder<GreetingComponentResponder>(GreetingComponentKeys.NoNicknameMatches);
        services.AddComponentResponder<GreetingComponentResponder>(GreetingComponentKeys.SetAlternateRoleset);
        services.AddComponentResponder<GreetingComponentResponder>(GreetingComponentKeys.SetGuessedNickname);
        services.AddComponentResponder<GreetingDeleteAltRolesetResponder>(GreetingComponentKeys.DeleteAlternateRolesets);

        services.AddCommandTree()
                .WithCommandGroup<GreetingCommands>()
                .Finish();

        return services;
    }
}
