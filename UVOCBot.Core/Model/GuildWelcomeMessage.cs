using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UVOCBot.Core.Model;

/// <summary>
/// Contains settings for the welcome message feature
/// </summary>
public class GuildWelcomeMessage : IGuildObject
{
    /// <inheritdoc />
    [Key]
    public ulong GuildId { get; set; }

    /// <summary>
    /// Gets or sets the label that is shown on the button to assign the alternate roles.
    /// </summary>
    public string AlternateRoleLabel { get; set; }

    /// <summary>
    /// Gets the list of alternate roles that can be assigned.
    /// </summary>
    public List<ulong> AlternateRoles { get; set; }

    /// <summary>
    /// Gets or sets the channel in which to send the welcome message.
    /// </summary>
    public ulong ChannelId { get; set; }

    /// <summary>
    /// Gets the list of default roles to assign.
    /// </summary>
    public List<ulong> DefaultRoles { get; set; }

    /// <summary>
    /// Gets or sets a value indicating if an attempt will be made to provide nickname options, based off the last few in-game joins.
    /// </summary>
    public bool DoIngameNameGuess { get; set; }

    /// <summary>
    /// Gets or sets a value indicating if this feature is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the message to send when a new user joins the server.
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Gets or sets a value indicating if the alternate roles should be offered.
    /// </summary>
    public bool OfferAlternateRoles { get; set; }

    /// <summary>
    /// Gets or sets the tag to use for making nickname guesses.
    /// </summary>
    public ulong OutfitId { get; set; }

    /// <summary>
    /// Only use this constructor if you are setting the <see cref="GuildId"/> immediately after construction.
    /// </summary>
    public GuildWelcomeMessage()
        : this(0)
    {
    }

    public GuildWelcomeMessage(ulong guildId)
    {
        AlternateRoleLabel = string.Empty;
        AlternateRoles = new List<ulong>();
        DefaultRoles = new List<ulong>();
        DoIngameNameGuess = false;
        GuildId = guildId;
        IsEnabled = false;
        Message = "Welcome <name>!";
    }
}
