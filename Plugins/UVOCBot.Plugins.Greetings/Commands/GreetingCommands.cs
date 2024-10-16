﻿using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Rest.Core;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using UVOCBot.Core;
using UVOCBot.Core.Extensions;
using UVOCBot.Core.Model;
using UVOCBot.Discord.Core;
using UVOCBot.Discord.Core.Abstractions.Services;
using UVOCBot.Discord.Core.Commands;
using UVOCBot.Discord.Core.Commands.Attributes;
using UVOCBot.Discord.Core.Commands.Conditions.Attributes;
using UVOCBot.Discord.Core.Errors;
using UVOCBot.Plugins.Greetings.Abstractions.Services;

namespace UVOCBot.Plugins.Greetings.Commands;

[Group("greeting")]
[Description("Commands that allow the greetings feature to be setup")]
[RequireContext(ChannelContext.Guild)]
[RequireGuildPermission(DiscordPermission.ManageGuild, IncludeSelf = false)]
[Deferred]
public class GreetingCommands : CommandGroup
{
    private readonly IInteraction _context;
    private readonly IDiscordRestChannelAPI _channelApi;
    private readonly IGreetingService _greetingService;
    private readonly IPermissionChecksService _permissionChecksService;
    private readonly DiscordContext _dbContext;
    private readonly FeedbackService _feedbackService;

    public GreetingCommands
    (
        IInteractionContext context,
        IDiscordRestChannelAPI channelApi,
        IGreetingService greetingService,
        IPermissionChecksService permissionChecksService,
        DiscordContext dbContext,
        FeedbackService responder
    )
    {
        _context = context.Interaction;
        _feedbackService = responder;
        _channelApi = channelApi;
        _greetingService = greetingService;
        _dbContext = dbContext;
        _permissionChecksService = permissionChecksService;
    }

    [Command("enabled")]
    [Description("Enables or disables the entire welcome message feature.")]
    public async Task<Result> EnabledCommand([Description("True to enable the welcome message feature.")] bool isEnabled)
    {
        GuildWelcomeMessage welcomeMessage = await GetWelcomeMessage().ConfigureAwait(false);
        welcomeMessage.IsEnabled = isEnabled;

        _dbContext.Update(welcomeMessage);
        await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

        return await _feedbackService.SendContextualSuccessAsync
        (
            $"Greetings have been {Formatter.Bold(isEnabled ? "enabled" : "disabled")}.",
            ct: CancellationToken
        ).ConfigureAwait(false);
    }

    [Command("add-alternate-roleset")]
    [Description("Adds an alternate roleset that will be offered to the new member, in place of the default roleset.")]
    [RequireGuildPermission(DiscordPermission.ManageRoles)]
    public async Task<Result> AddAlternateRolesetCommandAsync
    (
        [Description("The description of the roleset, shown when the user selects the set.")] string label,
        [Description("The first role to grant the user.")][DiscordTypeHint(TypeHint.Role)] Snowflake role1,
        [Description("Optional. The second role to grant the user.")][DiscordTypeHint(TypeHint.Role)] Snowflake? role2 = null,
        [Description("Optional. The third role to grant the user.")][DiscordTypeHint(TypeHint.Role)] Snowflake? role3 = null
    )
    {
        if (label.Length > 80)
            return new GenericCommandError("The description must be no longer than 80 characters");

        GuildWelcomeMessage welcomeMessage = await GetWelcomeMessage().ConfigureAwait(false);
        if (welcomeMessage.AlternateRolesets.Count >= 5)
            return new GenericCommandError("You can add a maximum of 5 alternate rolesets. Please remove an existing set before adding this new one.");

        List<ulong> roleIDs = new() { role1.Value };
        if (role2.HasValue)
            roleIDs.Add(role2.Value.Value);
        if (role3.HasValue)
            roleIDs.Add(role3.Value.Value);

        GuildGreetingAlternateRoleSet roleset = new
        (
            Snowflake.CreateTimestampSnowflake(DateTimeOffset.UtcNow, Constants.DiscordEpoch).Value,
            label,
            roleIDs
        );

        welcomeMessage.AlternateRolesets.Add(roleset);
        _dbContext.Update(welcomeMessage);
        await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

        return await _feedbackService.SendContextualSuccessAsync("Roleset added successfully!", ct: CancellationToken);
    }

    [Command("delete-alternate-rolesets")]
    [Description("Allows a selection of alternate rolesets to be deleted.")]
    [RequireGuildPermission(DiscordPermission.ManageRoles)]
    public async Task<Result> DeleteAlternateRolesetsCommandAsync()
    {
        GuildWelcomeMessage welcomeMessage = await GetWelcomeMessage().ConfigureAwait(false);
        if (welcomeMessage.AlternateRolesets.Count == 0)
            return new GenericCommandError("You must add at least one roleset before you can remove any.");

        ISelectOption[] selectOptions = _greetingService.CreateAlternateRoleSelectOptions(welcomeMessage.AlternateRolesets);
        StringSelectComponent alternateRoleSelectMenu = new
        (
            GreetingComponentKeys.DeleteAlternateRolesets,
            selectOptions,
            "Select rolesets to delete",
            0,
            selectOptions.Length
        );

        Result t = await _feedbackService.SendContextualNeutralAsync
        (
            "Select rolesets to delete",
            options: new FeedbackMessageOptions
            (
                MessageComponents: new IMessageComponent[] {
                    new ActionRowComponent(new[] { alternateRoleSelectMenu })
                }
            ),
            ct: CancellationToken
        );

        return t;
    }

    [Command("channel")]
    [Description("Sets the channel to post the welcome message in.")]
    public async Task<Result> ChannelCommand([ChannelTypes(ChannelType.GuildText)] IChannel channel)
    {
        Result<IDiscordPermissionSet> getPermissionSet = await _permissionChecksService.GetPermissionsInChannel
        (
            channel,
            DiscordConstants.UserId,
            CancellationToken
        ).ConfigureAwait(false);

        if (!getPermissionSet.IsSuccess)
            return (Result)getPermissionSet;

        if (!getPermissionSet.Entity.HasAdminOrPermission(DiscordPermission.ViewChannel))
            return new PermissionError(DiscordPermission.ViewChannel, DiscordConstants.UserId, channel.ID);

        if (!getPermissionSet.Entity.HasAdminOrPermission(DiscordPermission.SendMessages))
            return new PermissionError(DiscordPermission.SendMessages, DiscordConstants.UserId, channel.ID);

        if (!getPermissionSet.Entity.HasAdminOrPermission(DiscordPermission.ManageRoles))
            return new PermissionError(DiscordPermission.ManageRoles, DiscordConstants.UserId, channel.ID);

        if (!getPermissionSet.Entity.HasAdminOrPermission(DiscordPermission.ChangeNickname))
            return new PermissionError(DiscordPermission.ChangeNickname, DiscordConstants.UserId, channel.ID);

        GuildWelcomeMessage welcomeMessage = await _dbContext.FindOrDefaultAsync<GuildWelcomeMessage>
        (
            _context.GuildID.Value.Value,
            ct: CancellationToken
        ).ConfigureAwait(false);

        welcomeMessage.ChannelId = channel.ID.Value;

        _dbContext.Update(welcomeMessage);
        await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

        IResult responseResult = await _feedbackService.SendContextualSuccessAsync
        (
            $"The welcome message will now be posted in { Formatter.ChannelMention(channel.ID.Value) }.",
            ct: CancellationToken
        ).ConfigureAwait(false);

        return !responseResult.IsSuccess
            ? (Result)responseResult
            : Result.FromSuccess();
    }

    [Command("default-roles")]
    [Description("Provides the new member default roles.")]
    [RequireGuildPermission(DiscordPermission.ManageRoles)]
    public async Task<Result> DefaultRolesCommand
    (
        [Description("The roles to apply. Leave empty to apply no roles.")] string? roles = null
    )
    {
        GuildWelcomeMessage welcomeMessage = await GetWelcomeMessage().ConfigureAwait(false);

        Result replyResult;
        if (string.IsNullOrEmpty(roles))
        {
            welcomeMessage.DefaultRoles.Clear();
            replyResult = await _feedbackService.SendContextualSuccessAsync("No roles will be assigned by default.", ct: CancellationToken);
        }
        else
        {
            List<ulong> roleIds = ParseRoles(roles).ToList();

            Result canManipulateRoles = await _permissionChecksService.CanManipulateRoles(_context.GuildID.Value, roleIds);
            if (!canManipulateRoles.IsSuccess)
                return canManipulateRoles;

            welcomeMessage.DefaultRoles = roleIds;

            replyResult = await _feedbackService.SendContextualSuccessAsync
            (
                "The following roles will be assigned when a new member requests alternate roles: "
                    + string.Join(' ', roleIds.Select(Formatter.RoleMention)),
                ct: CancellationToken
            );
        }

        _dbContext.Update(welcomeMessage);
        await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

        return replyResult;
    }

    [Command("message")]
    [Description("Sets the message to present to new members. Use without arguments for more information.")]
    public async Task<Result> MessageCommand
    (
        [Description("The ID of the message to replicate as the welcome message.")] Snowflake? messageId = null
    )
    {
        if (messageId is null)
        {
            // Return info
            return await _feedbackService.SendContextualInfoAsync
            (
                @$"This command requires you to post the message you'd like to set as the welcome message. You can do this anywhere you like.

                Then, copy the ID by right-clicking said message, and re-run this command while supplying the ID.
                Make sure you run this command in the same channel that you posted the message in.

                {Formatter.Emoji("bulb")} Note that you can use { Formatter.InlineQuote("<name>") } as a placeholder for the joining member's name.",
                ct: CancellationToken
            ).ConfigureAwait(false);
        }

        Result<IMessage> getMessageResult = await _channelApi.GetChannelMessageAsync
        (
            _context.Channel.Value.ID.Value,
            (Snowflake)messageId,
            CancellationToken
        ).ConfigureAwait(false);

        if (!getMessageResult.IsSuccess)
        {
            return new GenericCommandError
            (
                "I couldn't find that message. Make sure you use this command in the same channel " +
                "as you sent the message, and that you've provided the right ID."
            );
        }

        GuildWelcomeMessage welcomeMessage = await GetWelcomeMessage().ConfigureAwait(false);
        welcomeMessage.Message = getMessageResult.Entity.Content;

        _dbContext.Update(welcomeMessage);
        await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

        return await _feedbackService.SendContextualSuccessAsync("The greeting message has been successfully updated!", ct: CancellationToken).ConfigureAwait(false);
    }

    [Command("test")]
    [Description("Tests the greeting feature with your current setup.")]
    public async Task<Result> TestCommand
    (
        [Description("Optional: the member to target with the greeting.")] IGuildMember? target = null
    )
    {
        Result greetingResult = await _greetingService.SendGreeting
        (
            _context.GuildID.Value,
            target ?? _context.Member.Value,
            CancellationToken
        ).ConfigureAwait(false);

        return !greetingResult.IsSuccess
            ? greetingResult
            : await _feedbackService.SendContextualSuccessAsync("Greeting sent.", ct: CancellationToken).ConfigureAwait(false);
    }

    private static List<ulong> ParseRoles(string roles)
    {
        List<ulong> roleIDs = [];

        foreach (string role in roles.Split("<@&", StringSplitOptions.RemoveEmptyEntries))
        {
            int index = role.IndexOf('>');

            if (index > 0 && ulong.TryParse(role[..index], out ulong roleID))
                roleIDs.Add(roleID);
        }

        return roleIDs;
    }

    private async ValueTask<GuildWelcomeMessage> GetWelcomeMessage()
        => await _dbContext.FindOrDefaultAsync<GuildWelcomeMessage>(_context.GuildID.Value.Value, ct: CancellationToken);
}
