﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UVOCBot.Core.Model;

/// <summary>
/// Contains settings for the welcome message feature
/// </summary>
public class GuildWelcomeMessage : IGuildObject
{
    /// <inheritdoc />
    [Key]
    public ulong GuildId { get; init; }

    /// <summary>
    /// Gets the list of alternate roles that can be assigned.
    /// </summary>
    public List<GuildGreetingAlternateRoleSet> AlternateRolesets { get; }

    /// <summary>
    /// Gets or sets the channel in which to send the welcome message.
    /// </summary>
    public ulong ChannelId { get; set; }

    /// <summary>
    /// Gets the list of default roles to assign.
    /// </summary>
    public List<ulong> DefaultRoles { get; set; }

    /// <summary>
    /// Gets or sets a value indicating if this feature is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the message to send when a new user joins the server.
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Only use this constructor if you are setting the <see cref="GuildId"/> immediately after construction.
    /// </summary>
    public GuildWelcomeMessage()
        : this(0)
    {
    }

    public GuildWelcomeMessage(ulong guildId)
    {
        AlternateRolesets = new List<GuildGreetingAlternateRoleSet>();
        DefaultRoles = new List<ulong>();
        GuildId = guildId;
        IsEnabled = false;
        Message = "Welcome <name>!";
    }
}

public record GuildGreetingAlternateRoleSet
(
    ulong ID,
    string Description,
    IReadOnlyList<ulong> RoleIDs
);
