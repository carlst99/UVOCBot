﻿using Microsoft.Extensions.DependencyInjection;
using Remora.Commands.Extensions;
using UVOCBot.Discord.Core.Extensions;
using UVOCBot.Plugins.Roles;
using UVOCBot.Plugins.Roles.Abstractions.Services;
using UVOCBot.Plugins.Roles.Commands;
using UVOCBot.Plugins.Roles.Responders;
using UVOCBot.Plugins.Roles.Services;

namespace UVOCBot.Plugins;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddRolesPlugin(this IServiceCollection services)
    {
        services.AddScoped<IRoleMenuService, RoleMenuService>();

        services.AddComponentResponder<EditMenuModalResponder>(RoleComponentKeys.ModalEditMenu);
        services.AddComponentResponder<ToggleRoleComponentResponder>(RoleComponentKeys.ConfirmDeletion);
        services.AddComponentResponder<ToggleRoleComponentResponder>(RoleComponentKeys.ToggleRole);

        services.AddCommandTree()
                .WithCommandGroup<RoleCommands>()
                .WithCommandGroup<RoleMenuCommands>()
                .Finish();

        return services;
    }
}
