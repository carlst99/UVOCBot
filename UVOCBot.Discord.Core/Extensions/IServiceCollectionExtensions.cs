using Microsoft.Extensions.DependencyInjection;
using Remora.Commands.Extensions;
using UVOCBot.Discord.Core.Commands.Conditions;

namespace UVOCBot.Discord.Core.Extensions
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddCoreDiscordServices(this IServiceCollection services)
        {
            services.AddCondition<RequireContextCondition>();
            services.AddCondition<RequireGuildPermissionCondition>();

            return services;
        }
    }
}
