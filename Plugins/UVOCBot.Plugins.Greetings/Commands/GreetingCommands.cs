﻿using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Rest.Core;
using Remora.Results;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using UVOCBot.Core;
using UVOCBot.Core.Model;
using UVOCBot.Discord.Core;
using UVOCBot.Discord.Core.Commands.Conditions.Attributes;
using UVOCBot.Discord.Core.Errors;
using UVOCBot.Discord.Core.Services.Abstractions;
using UVOCBot.Plugins.Greetings.Abstractions.Services;
using UVOCBot.Plugins.Greetings.Objects;

namespace UVOCBot.Plugins.Greetings.Commands;

[Group("greeting")]
[Description("Commands that allow the greetings feature to be setup")]
[RequireContext(ChannelContext.Guild)]
[RequireGuildPermission(DiscordPermission.ManageGuild, false)]
public class GreetingCommands : CommandGroup
{
    private readonly ICommandContext _context;
    private readonly ICensusQueryService _censusService;
    private readonly IDiscordRestChannelAPI _channelApi;
    private readonly IDiscordRestGuildAPI _guildApi;
    private readonly IGreetingService _greetingService;
    private readonly IPermissionChecksService _permissionChecksService;
    private readonly DiscordContext _dbContext;
    private readonly FeedbackService _feedbackService;

    public GreetingCommands
    (
        ICommandContext context,
        ICensusQueryService censusService,
        IDiscordRestChannelAPI channelApi,
        IDiscordRestGuildAPI guildApi,
        IGreetingService greetingService,
        IPermissionChecksService permissionChecksService,
        DiscordContext dbContext,
        FeedbackService responder
    )
    {
        _context = context;
        _censusService = censusService;
        _feedbackService = responder;
        _channelApi = channelApi;
        _guildApi = guildApi;
        _greetingService = greetingService;
        _dbContext = dbContext;
        _permissionChecksService = permissionChecksService;
    }

    [Command("enabled")]
    [Description("Enables or disables the entire welcome message feature.")]
    public async Task<IResult> EnabledCommand([Description("True to enable the welcome message feature.")] bool isEnabled)
    {
        GuildWelcomeMessage welcomeMessage = await GetWelcomeMessage().ConfigureAwait(false);

        welcomeMessage.IsEnabled = isEnabled;

        _dbContext.Update(welcomeMessage);
        await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

        return await _feedbackService.SendContextualSuccessAsync
        (
            $"The welcome message feature has been {Formatter.Bold(isEnabled ? "enabled" : "disabled")}.",
            ct: CancellationToken
        ).ConfigureAwait(false);
    }

    [Command("alternate-roles")]
    [Description("Provides the new member with the option to give themself an alternative set of roles.")]
    [RequireGuildPermission(DiscordPermission.ManageRoles)]
    public async Task<IResult> AlternateRolesCommand
    (
        [Description("Set whether the alternate roles will be offered.")] bool offerAlternateRoles,
        [Description("The label of the alternate role button.")] string? alternateRoleButtonLabel = null,
        [Description("The roles to apply.")] string? roles = null
    )
    {
        GuildWelcomeMessage welcomeMessage = await GetWelcomeMessage().ConfigureAwait(false);
        welcomeMessage.OfferAlternateRoles = offerAlternateRoles;

        if (offerAlternateRoles && string.IsNullOrEmpty(alternateRoleButtonLabel))
            return await _feedbackService.SendContextualErrorAsync("You must set a label.", ct: CancellationToken).ConfigureAwait(false);

        if (offerAlternateRoles && string.IsNullOrEmpty(roles))
            return await _feedbackService.SendContextualErrorAsync("You must provide some roles.", ct: CancellationToken).ConfigureAwait(false);

        IResult replyResult;
        if (!offerAlternateRoles)
        {
            replyResult = await _feedbackService.SendContextualSuccessAsync
            (
                "Alternate roles will not be offered to new members.",
                ct: CancellationToken
            ).ConfigureAwait(false);
        }
        else
        {
            IEnumerable<ulong> roleIds = ParseRoles(roles!);
            IResult canManipulateRoles = await _permissionChecksService.CanManipulateRoles(_context.GuildID.Value, roleIds).ConfigureAwait(false);
            if (!canManipulateRoles.IsSuccess)
                return canManipulateRoles;

            welcomeMessage.AlternateRoleLabel = alternateRoleButtonLabel ?? string.Empty;
            welcomeMessage.AlternateRoles = roleIds.ToList();

            replyResult = await _feedbackService.SendContextualSuccessAsync
            (
                "The following roles will be assigned when a new member requests alternate roles: " + string.Join(' ', roleIds.Select(r => Formatter.RoleMention(r))),
                ct: CancellationToken
            ).ConfigureAwait(false);
        }

        _dbContext.Update(welcomeMessage);
        await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

        return replyResult;
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
            return Result.FromError(getPermissionSet);

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
            CancellationToken
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
            ? Result.FromError(responseResult.Error!)
            : Result.FromSuccess();
    }

    [Command("default-roles")]
    [Description("Provides the new member default roles.")]
    [RequireGuildPermission(DiscordPermission.ManageRoles)]
    public async Task<IResult> DefaultRolesCommand
    (
        [Description("The roles to apply. Leave empty to apply no roles.")] string? roles
    )
    {
        GuildWelcomeMessage welcomeMessage = await GetWelcomeMessage().ConfigureAwait(false);

        IResult replyResult;
        if (string.IsNullOrEmpty(roles))
        {
            welcomeMessage.DefaultRoles.Clear();
            replyResult = await _feedbackService.SendContextualSuccessAsync("No roles will be assigned by default.", ct: CancellationToken).ConfigureAwait(false);
        }
        else
        {
            IEnumerable<ulong> roleIds = ParseRoles(roles);
            IResult canManipulateRoles = await _permissionChecksService.CanManipulateRoles(_context.GuildID.Value, roleIds).ConfigureAwait(false);
            if (!canManipulateRoles.IsSuccess)
                return canManipulateRoles;

            welcomeMessage.DefaultRoles = roleIds.ToList();

            replyResult = await _feedbackService.SendContextualSuccessAsync
            (
                "The following roles will be assigned when a new member requests alternate roles: " + string.Join(' ', roleIds.Select(r => Formatter.RoleMention(r))),
                ct: CancellationToken
            ).ConfigureAwait(false);
        }

        _dbContext.Update(welcomeMessage);
        await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

        return replyResult;
    }

    [Command("ingame-name-guess")]
    [Description("Attempts to guess the new member's in-game name, in order to make an offer to set their nickname.")]
    [RequireGuildPermission(DiscordPermission.ChangeNickname)]
    public async Task<IResult> IngameNameGuessCommand
    (
        [Description("Is the nickname guess feature enabled.")] bool isEnabled,
        [Description("The tag of the outfit to make nickname guesses from, based on its newest members.")] string? outfitTag = null
    )
    {
        if (isEnabled && string.IsNullOrEmpty(outfitTag))
        {
            return await _feedbackService.SendContextualErrorAsync
            (
                "You must provide an outfit tag to enable the name guess feature.",
                ct: CancellationToken
            ).ConfigureAwait(false);
        }

        GuildWelcomeMessage welcomeMessage = await GetWelcomeMessage().ConfigureAwait(false);
        welcomeMessage.DoIngameNameGuess = isEnabled;

        IResult replyResult;
        if (!isEnabled)
        {
            welcomeMessage.DoIngameNameGuess = false;
            replyResult = await _feedbackService.SendContextualSuccessAsync
            (
                $"In-game name guesses will { Formatter.Italic("not") } be made.",
                ct: CancellationToken
            ).ConfigureAwait(false);
        }
        else
        {
            Result<Outfit?> getOutfit = await _censusService.GetOutfitAsync(outfitTag!, CancellationToken).ConfigureAwait(false);
            if (!getOutfit.IsSuccess)
                return getOutfit;

            if (getOutfit.Entity is null)
                return await _feedbackService.SendContextualErrorAsync("That outfit does not exist.", ct: CancellationToken).ConfigureAwait(false);

            welcomeMessage.OutfitId = getOutfit.Entity.OutfitId;

            replyResult = await _feedbackService.SendContextualSuccessAsync
            (
                $"Nickname guesses from the outfit { Formatter.Bold(getOutfit.Entity.Name) } will now be presented on the welcome message.",
                ct: CancellationToken
            ).ConfigureAwait(false);
        }

        _dbContext.Update(welcomeMessage);
        await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

        return replyResult;
    }

    [Command("message")]
    [Description("Sets the message to present to new members. Use without arguments for more information.")]
    public async Task<IResult> MessageCommand
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
                Make sure you do this in the same channel that you posted the message in.
                Note that you can use { Formatter.InlineQuote("<name>") } as a placeholder for the joining member's name.",
                ct: CancellationToken
            ).ConfigureAwait(false);
        }

        Result<IMessage> getMessageResult = await _channelApi.GetChannelMessageAsync(_context.ChannelID, (Snowflake)messageId, CancellationToken).ConfigureAwait(false);
        if (!getMessageResult.IsSuccess)
        {
            await _feedbackService.SendContextualErrorAsync
            (
                "I couldn't find that message. Make sure you use this command in the same channel as you sent the message, and that you've provided the right ID.",
                ct: CancellationToken
            ).ConfigureAwait(false);
            return getMessageResult;
        }

        GuildWelcomeMessage welcomeMessage = await GetWelcomeMessage().ConfigureAwait(false);
        welcomeMessage.Message = getMessageResult.Entity.Content;

        _dbContext.Update(welcomeMessage);
        await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

        return await _feedbackService.SendContextualSuccessAsync("Message successfully updated!", ct: CancellationToken).ConfigureAwait(false);
    }

#if DEBUG
    [Command("test")]
    [Description("Tests the welcome message feature.")]
    public async Task<IResult> TestCommand()
    {
        if (_context is not InteractionContext ictx)
            return await _feedbackService.SendContextualErrorAsync("This command can only be used as a slash command.", ct: CancellationToken).ConfigureAwait(false);

        IResult greetingResult = await _greetingService.SendGreeting(_context.GuildID.Value, ictx.Member.Value, CancellationToken).ConfigureAwait(false);

        return !greetingResult.IsSuccess
            ? greetingResult
            : await _feedbackService.SendContextualSuccessAsync("Greeting sent.", ct: CancellationToken).ConfigureAwait(false);
    }
#endif

    private static IEnumerable<ulong> ParseRoles(string roles)
    {
        foreach (string role in roles.Split("<@&", System.StringSplitOptions.RemoveEmptyEntries))
        {
            int index = role.IndexOf('>');

            if (index > 0 && ulong.TryParse(role[0..index], out ulong roleId))
                yield return roleId;
        }
    }

    private async Task<GuildWelcomeMessage> GetWelcomeMessage()
        => await _dbContext.FindOrDefaultAsync<GuildWelcomeMessage>(_context.GuildID.Value.Value, CancellationToken).ConfigureAwait(false);
}
