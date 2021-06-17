using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Results;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using UVOCBot.Commands.Conditions.Attributes;
using UVOCBot.Core.Model;
using UVOCBot.Services.Abstractions;

namespace UVOCBot.Commands
{
    [Group("welcome-message")]
    [Description("Commands that allow the welcome message feature to be setup")]
    [RequireContext(ChannelContext.Guild)]
    [RequireUserGuildPermission(DiscordPermission.ManageGuild)]
    public class WelcomeMessageCommands : CommandGroup
    {
        private readonly ICommandContext _context;
        private readonly MessageResponseHelpers _responder;
        private readonly IDbApiService _dbApi;
        private readonly IDiscordRestGuildAPI _guildAPI;

        public WelcomeMessageCommands(ICommandContext context, MessageResponseHelpers responder, IDbApiService dbAPI, IDiscordRestGuildAPI guildAPI)
        {
            _context = context;
            _responder = responder;
            _dbApi = dbAPI;
            _guildAPI = guildAPI;
        }

        [Command("enabled")]
        [Description("Enables or disables the welcome message feature.")]
        public async Task<IResult> EnabledCommand([Description("True to enable the welcome message feature.")] bool isEnabled)
        {
            Result<GuildWelcomeMessageDto> welcomeMessage = await _dbApi.GetGuildWelcomeMessageAsync(_context.GuildID.Value.Value, CancellationToken).ConfigureAwait(false);
            if (!welcomeMessage.IsSuccess)
            {
                await _responder.RespondWithErrorAsync(_context, "Something went wrong! Please try again.", CancellationToken).ConfigureAwait(false);
                return welcomeMessage;
            }

            GuildWelcomeMessageDto returnValue = welcomeMessage.Entity with { IsEnabled = isEnabled };
            Result dbUpdateResult = await _dbApi.UpdateGuildWelcomeMessageAsync(_context.GuildID.Value.Value, returnValue, CancellationToken).ConfigureAwait(false);

            if (!dbUpdateResult.IsSuccess)
            {
                await _responder.RespondWithErrorAsync(_context, "Something went wrong! Please try again.", CancellationToken).ConfigureAwait(false);
                return dbUpdateResult;
            }
            else
            {
                return await _responder.RespondWithSuccessAsync(
                    _context,
                    "The welcome message feature has been " + Formatter.Bold(isEnabled ? "enabled." : "disabled."),
                    CancellationToken).ConfigureAwait(false);
            }
        }

        [Command("alternate-roles")]
        [Description("Provides the new member with the option to give themself an alternative set of roles.")]
        [RequireGuildPermission(DiscordPermission.ManageRoles)]
        public async Task<IResult> AlternateRolesCommand(
            [Description("The label to put on the button that lets the new member acquire the alternate roles.")] string alternateRoleButtonLabel,
            [Description("The roles to apply.")] string roles)
        {
            throw new NotImplementedException();
        }

        // TODO: When setting channel, ensure that we have the correct permission overwrites to adjust roles
    }
}
