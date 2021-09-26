using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Core;
using Remora.Results;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using UVOCBot.Core;
using UVOCBot.Core.Model;
using UVOCBot.Discord.Core;
using UVOCBot.Discord.Core.Commands.Conditions.Attributes;
using UVOCBot.Discord.Core.Services.Abstractions;
using UVOCBot.Services.Abstractions;

namespace UVOCBot.Commands
{
    [Group("welcome-message")]
    [Description("Commands that allow the welcome message feature to be setup")]
    [RequireContext(ChannelContext.Guild)]
    [RequireGuildPermission(DiscordPermission.ManageGuild, false)]
    public class WelcomeMessageCommands : CommandGroup
    {
        private readonly ICommandContext _context;
        private readonly DiscordContext _dbContext;
        private readonly ICensusApiService _censusApi;
        private readonly IDiscordRestChannelAPI _channelApi;
        private readonly IDiscordRestGuildAPI _guildApi;
        private readonly IPermissionChecksService _permissionChecksService;
        private readonly IReplyService _replyService;

        public WelcomeMessageCommands(
            ICommandContext context,
            DiscordContext dbContext,
            ICensusApiService censusApi,
            IDiscordRestChannelAPI channelApi,
            IDiscordRestGuildAPI guildApi,
            IPermissionChecksService permissionChecksService,
            IReplyService responder)
        {
            _context = context;
            _dbContext = dbContext;
            _censusApi = censusApi;
            _replyService = responder;
            _channelApi = channelApi;
            _guildApi = guildApi;
            _permissionChecksService = permissionChecksService;
        }

        [Command("enabled")]
        [Description("Enables or disables the entire welcome message feature.")]
        public async Task<IResult> EnabledCommand([Description("True to enable the welcome message feature.")] bool isEnabled)
        {
            GuildWelcomeMessage welcomeMessage = await _dbContext.FindOrDefaultAsync<GuildWelcomeMessage>(_context.GuildID.Value.Value, CancellationToken).ConfigureAwait(false);

            welcomeMessage.IsEnabled = isEnabled;

            _dbContext.Update(welcomeMessage);
            await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

            return await _replyService.RespondWithSuccessAsync(
                "The welcome message feature has been " + Formatter.Bold(isEnabled ? "enabled." : "disabled."),
                CancellationToken).ConfigureAwait(false);
        }

        [Command("alternate-roles")]
        [Description("Provides the new member with the option to give themself an alternative set of roles.")]
        [RequireGuildPermission(DiscordPermission.ManageRoles)]
        public async Task<IResult> AlternateRolesCommand(
            [Description("Set whether the alternate roles will be offered.")] bool offerAlternateRoles,
            [Description("The label of the alternate role button.")] string? alternateRoleButtonLabel = null,
            [Description("The roles to apply.")] string? roles = null)
        {
            GuildWelcomeMessage welcomeMessage = await _dbContext.FindOrDefaultAsync<GuildWelcomeMessage>(_context.GuildID.Value.Value, CancellationToken).ConfigureAwait(false);
            welcomeMessage.OfferAlternateRoles = offerAlternateRoles;

            if (offerAlternateRoles && string.IsNullOrEmpty(alternateRoleButtonLabel))
                return await _replyService.RespondWithUserErrorAsync("You must set a label.", CancellationToken).ConfigureAwait(false);

            if (offerAlternateRoles && string.IsNullOrEmpty(roles))
                return await _replyService.RespondWithUserErrorAsync("You must provide some roles.", CancellationToken).ConfigureAwait(false);

            Result<IMessage> replyResult;
            if (!offerAlternateRoles)
            {
                replyResult = await _replyService.RespondWithSuccessAsync("Alternate roles will not be offered to new members.", CancellationToken).ConfigureAwait(false);
            }
            else
            {
                IEnumerable<ulong> roleIds = ParseRoles(roles!);
                IResult canManipulateRoles = await _permissionChecksService.CanManipulateRoles(_context.GuildID.Value, roleIds).ConfigureAwait(false);
                if (!canManipulateRoles.IsSuccess)
                    return canManipulateRoles;

                welcomeMessage.AlternateRoleLabel = alternateRoleButtonLabel ?? string.Empty;
                welcomeMessage.AlternateRoles = roleIds.ToList();

                replyResult = await _replyService.RespondWithSuccessAsync(
                    "The following roles will be assigned when a new member requests alternate roles: " + string.Join(' ', roleIds.Select(r => Formatter.RoleMention(r))),
                    CancellationToken).ConfigureAwait(false);
            }

            _dbContext.Update(welcomeMessage);
            await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

            return replyResult;
        }

        [Command("channel")]
        [Description("Sets the channel to post the welcome message in.")]
        public async Task<IResult> ChannelCommand(IChannel channel)
        {
            Result<IDiscordPermissionSet> getPermissionSet = await _permissionChecksService.GetPermissionsInChannel(channel, BotConstants.UserId, CancellationToken).ConfigureAwait(false);
            if (!getPermissionSet.IsSuccess)
            {
                await _replyService.RespondWithErrorAsync(CancellationToken).ConfigureAwait(false);
                return getPermissionSet;
            }

            if (!getPermissionSet.Entity.HasAdminOrPermission(DiscordPermission.ViewChannel))
                return await _replyService.RespondWithUserErrorAsync("I do not have permission to view that channel.", CancellationToken).ConfigureAwait(false);

            if (!getPermissionSet.Entity.HasAdminOrPermission(DiscordPermission.SendMessages))
                return await _replyService.RespondWithUserErrorAsync("I do not have permission to send messages in this channel.", CancellationToken).ConfigureAwait(false);

            if (!getPermissionSet.Entity.HasAdminOrPermission(DiscordPermission.ManageRoles))
                return await _replyService.RespondWithUserErrorAsync("I do not have permission to manage roles in this channel.", CancellationToken).ConfigureAwait(false);

            if (!getPermissionSet.Entity.HasAdminOrPermission(DiscordPermission.ChangeNickname))
                return await _replyService.RespondWithUserErrorAsync("I do not have permission to change nicknames in this channel.", CancellationToken).ConfigureAwait(false);

            GuildWelcomeMessage welcomeMessage = await _dbContext.FindOrDefaultAsync<GuildWelcomeMessage>(_context.GuildID.Value.Value, CancellationToken).ConfigureAwait(false);
            welcomeMessage.ChannelId = channel.ID.Value;

            _dbContext.Update(welcomeMessage);
            await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

            return await _replyService.RespondWithSuccessAsync(
                $"The welcome message will now be posted in { Formatter.ChannelMention(channel.ID.Value) }.",
                CancellationToken).ConfigureAwait(false);
        }

        [Command("default-roles")]
        [Description("Provides the new member default roles.")]
        [RequireGuildPermission(DiscordPermission.ManageRoles)]
        public async Task<IResult> DefaultRolesCommand(
            [Description("The roles to apply. Leave empty to apply no roles.")] string? roles)
        {
            GuildWelcomeMessage welcomeMessage = await _dbContext.FindOrDefaultAsync<GuildWelcomeMessage>(_context.GuildID.Value.Value, CancellationToken).ConfigureAwait(false);

            Result<IMessage> replyResult;
            if (string.IsNullOrEmpty(roles))
            {
                welcomeMessage.DefaultRoles.Clear();
                replyResult = await _replyService.RespondWithUserErrorAsync("No roles will be assigned by default.", CancellationToken).ConfigureAwait(false);
            }
            else
            {
                IEnumerable<ulong> roleIds = ParseRoles(roles);
                IResult canManipulateRoles = await _permissionChecksService.CanManipulateRoles(_context.GuildID.Value, roleIds).ConfigureAwait(false);
                if (!canManipulateRoles.IsSuccess)
                    return canManipulateRoles;

                welcomeMessage.DefaultRoles = roleIds.ToList();

                replyResult = await _replyService.RespondWithSuccessAsync(
                    "The following roles will be assigned when a new member requests alternate roles: " + string.Join(' ', roleIds.Select(r => Formatter.RoleMention(r))),
                    CancellationToken).ConfigureAwait(false);
            }

            _dbContext.Update(welcomeMessage);
            await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

            return replyResult;
        }

        [Command("ingame-name-guess")]
        [Description("Attempts to guess the new member's in-game name, in order to make an offer to set their nickname.")]
        [RequireGuildPermission(DiscordPermission.ChangeNickname)]
        public async Task<IResult> IngameNameGuessCommand(
            [Description("Is the nickname guess feature enabled.")] bool isEnabled,
            [Description("The tag of the outfit to make nickname guesses from, based on its newest members.")] string? outfitTag = null)
        {
            if (isEnabled && string.IsNullOrEmpty(outfitTag))
                return await _replyService.RespondWithUserErrorAsync("You must provide an outfit tag to enable the name guess feature.", CancellationToken).ConfigureAwait(false);

            GuildWelcomeMessage welcomeMessage = await _dbContext.FindOrDefaultAsync<GuildWelcomeMessage>(_context.GuildID.Value.Value, CancellationToken).ConfigureAwait(false);
            welcomeMessage.DoIngameNameGuess = isEnabled;

            Result<IMessage> replyResult;
            if (!isEnabled)
            {
                welcomeMessage.DoIngameNameGuess = false;
                replyResult = await _replyService.RespondWithSuccessAsync($"In-game name guesses will { Formatter.Italic("not") } be made.", CancellationToken).ConfigureAwait(false);
            }
            else
            {
                Result<Outfit?> getOutfit = await _censusApi.GetOutfitAsync(outfitTag!, CancellationToken).ConfigureAwait(false);
                if (!getOutfit.IsSuccess)
                {
                    await _replyService.RespondWithErrorAsync(CancellationToken).ConfigureAwait(false);
                    return getOutfit;
                }

                if (getOutfit.Entity is null)
                    return await _replyService.RespondWithUserErrorAsync("That outfit does not exist.", CancellationToken).ConfigureAwait(false);

                welcomeMessage.OutfitId = getOutfit.Entity.OutfitId;

                replyResult = await _replyService.RespondWithSuccessAsync(
                    $"Nickname guesses from the outfit { Formatter.Bold(getOutfit.Entity.Name) } will now be presented on the welcome message.",
                    CancellationToken).ConfigureAwait(false);
            }

            _dbContext.Update(welcomeMessage);
            await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

            return replyResult;
        }

        [Command("message")]
        [Description("Sets the message to present to new members. Use without arguments for more information.")]
        public async Task<IResult> MessageCommand(
            [Description("The ID of the message to replicate as the welcome message.")] Snowflake? messageId = null)
        {
            if (messageId is null)
            {
                // Return info
                return await _replyService.RespondWithSuccessAsync
                (
                    "This command requires you to post the message you'd like to set as the welcome message. You can do this anywhere you like." +
                    "\r\nThen, copy the ID by right-clicking said message, and re-run this command while supplying the ID. Make sure you do this in the same channel that you posted the message in." +
                    $"\r\nNote that you can use { Formatter.InlineQuote("<name>") } as a placeholder for the joining member's name.", CancellationToken
                ).ConfigureAwait(false);
            }

            Result<IMessage> getMessageResult = await _channelApi.GetChannelMessageAsync(_context.ChannelID, (Snowflake)messageId, CancellationToken).ConfigureAwait(false);
            if (!getMessageResult.IsSuccess)
            {
                await _replyService.RespondWithUserErrorAsync
                (
                    "I couldn't find that message. Make sure you use this command in the same channel as you sent the message, and that you've provided the right ID.",
                    CancellationToken
                ).ConfigureAwait(false);
                return getMessageResult;
            }

            GuildWelcomeMessage welcomeMessage = await _dbContext.FindOrDefaultAsync<GuildWelcomeMessage>(_context.GuildID.Value.Value, CancellationToken).ConfigureAwait(false);
            welcomeMessage.Message = getMessageResult.Entity.Content;

            _dbContext.Update(welcomeMessage);
            await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

            return await _replyService.RespondWithSuccessAsync("Message successfully updated!", CancellationToken).ConfigureAwait(false);
        }

        private static IEnumerable<ulong> ParseRoles(string roles)
        {
            foreach (string role in roles.Split("<@&", System.StringSplitOptions.RemoveEmptyEntries))
            {
                int index = role.IndexOf('>');

                if (index > 0 && ulong.TryParse(role[0..index], out ulong roleId))
                    yield return roleId;
            }
        }
    }
}
