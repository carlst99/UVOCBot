using Microsoft.Extensions.DependencyInjection;
using Remora.Commands.Extensions;
using UVOCBot.Discord.Core.Extensions;
using UVOCBot.Plugins.Roles;
using UVOCBot.Plugins.Roles.Commands;
using UVOCBot.Plugins.Roles.Responders;

namespace UVOCBot.Plugins;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddRolesPlugin(this IServiceCollection services)
    {
        services.AddComponentResponder<ToggleRoleComponentResponder>(RoleComponentKeys.ConfirmDeletion);
        services.AddComponentResponder<ToggleRoleComponentResponder>(RoleComponentKeys.ToggleRole);

        services.AddCommandTree()
                .WithCommandGroup<RoleCommands>()
                .WithCommandGroup<RoleMenuCommands>()
                .Finish();

        return services;
    }
}
