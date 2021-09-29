using Microsoft.Extensions.DependencyInjection;
using Remora.Commands.Extensions;
using Remora.Discord.Commands.Extensions;
using UVOCBot.Discord.Core.Commands.Conditions;
using UVOCBot.Discord.Core.ExecutionEvents;
using UVOCBot.Discord.Core.Services;
using UVOCBot.Discord.Core.Services.Abstractions;

namespace UVOCBot.Discord.Core.Extensions
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddCoreDiscordServices(this IServiceCollection services)
        {
            services.AddScoped<IPermissionChecksService, PermissionChecksService>(); // TODO: Can make this singleton after service refactor

            services.AddCondition<RequireContextCondition>();
            services.AddCondition<RequireGuildPermissionCondition>();

            services.AddPostExecutionEvent<ErrorFeedbackPostExecutionEvent>();
            services.AddPostExecutionEvent<ErrorLogPostExecutionEvent>();

            return services;
        }
    }
}
