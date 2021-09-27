using Microsoft.Extensions.DependencyInjection;
using Remora.Commands.Extensions;
using Remora.Discord.Commands.Extensions;
using UVOCBot.Discord.Core.Commands.Conditions;
using UVOCBot.Discord.Core.ExecutionEvents;

namespace UVOCBot.Discord.Core.Extensions
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddCoreDiscordServices(this IServiceCollection services)
        {
            services.AddCondition<RequireContextCondition>();
            services.AddCondition<RequireGuildPermissionCondition>();

            services.AddPostExecutionEvent<UserErrorPostExecutionEvent>();
            services.AddPostExecutionEvent<ErrorLogPostExecutionEvent>();

            return services;
        }
    }
}
