using Remora.Commands.Conditions;
using Remora.Discord.API.Abstractions.Objects;
using System;

namespace UVOCBot.Discord.Core.Commands.Conditions.Attributes;

/// <summary>
/// Marks a command as requiring the both the bot user and requesting user to have a particular permission within the guild.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireGuildPermissionAttribute : ConditionAttribute
{
    /// <summary>
    /// Gets the permission.
    /// </summary>
    public DiscordPermission Permission { get; }

    /// <summary>
    /// Gets a value indicating if the current user (i.e. the bot) will also be included in the permission checks.
    /// </summary>
    public bool IncludeCurrent { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RequireGuildPermissionAttribute"/> class.
    /// </summary>
    /// <param name="permission">The permission.</param>
    /// <param name="includeCurrent">Indicates that the current user (i.e. the bot) will also be included in the checks.</param>
    public RequireGuildPermissionAttribute(DiscordPermission permission, bool includeCurrent = true)
    {
        Permission = permission;
        IncludeCurrent = includeCurrent;
    }
}
