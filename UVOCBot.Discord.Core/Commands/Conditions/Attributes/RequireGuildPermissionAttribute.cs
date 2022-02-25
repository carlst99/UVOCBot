using Remora.Commands.Conditions;
using Remora.Discord.API.Abstractions.Objects;
using System;
using System.Collections.Generic;

namespace UVOCBot.Discord.Core.Commands.Conditions.Attributes;

/// <summary>
/// Marks a command as requiring the executing user, and optionally the bot user, to have a particular permission within the guild.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireGuildPermissionAttribute : ConditionAttribute
{
    /// <summary>
    /// Gets the permission.
    /// </summary>
    public IReadOnlyList<DiscordPermission> RequiredPermissions { get; }

    /// <summary>
    /// Gets or sets a value indicating if the current user (i.e. the bot) will also be included in the permission checks.
    /// </summary>
    public bool IncludeSelf { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RequireGuildPermissionAttribute"/> class.
    /// </summary>
    /// <param name="requiredPermissions">The permission.</param>
    public RequireGuildPermissionAttribute(params DiscordPermission[] requiredPermissions)
    {
        RequiredPermissions = requiredPermissions;
    }
}
