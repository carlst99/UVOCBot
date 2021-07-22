using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Core;
using Remora.Results;
using System.ComponentModel;
using System.Threading.Tasks;
using UVOCBot.Commands.Conditions.Attributes;
using UVOCBot.Services.Abstractions;

namespace UVOCBot.Commands
{
    [Group("rolemenu")]
    [Description("Commands to create and edit role menus.")]
    [RequireContext(ChannelContext.Guild)]
    [RequireGuildPermission(DiscordPermission.ManageRoles)]
    [RequireGuildPermission(DiscordPermission.SendMessages)]
    public class RoleMenuCommands : CommandGroup
    {
        private readonly IDiscordRestChannelAPI _channelApi;
        private readonly IPermissionChecksService _permissionChecksService;
        private readonly IReplyService _replyService;

        public RoleMenuCommands(IDiscordRestChannelAPI channelApi, IPermissionChecksService permissionChecksService, IReplyService replyService)
        {
            _channelApi = channelApi;
            _permissionChecksService = permissionChecksService;
            _replyService = replyService;
        }

        [Command("create")]
        [Description("Creates a new role menu")]
        public async Task<IResult> CreateCommand(
            [Description("The channel to post the role menu in.")] IChannel channel,
            [Description("The title of the role menu.")] string title,
            [Description("The description of the role menu.")] string? description = null)
        {
            Result<IDiscordPermissionSet> permissionsResult = await _permissionChecksService.GetPermissionsInChannel(channel.ID, BotConstants.UserId, CancellationToken).ConfigureAwait(false);
            if (!permissionsResult.IsSuccess)
            {
                await _replyService.RespondWithErrorAsync(CancellationToken).ConfigureAwait(false);
                return permissionsResult;
            }

            if (!permissionsResult.Entity.HasPermission(DiscordPermission.ManageRoles))
                return await _replyService.RespondWithUserErrorAsync("I don't have permission to manipulate roles in that channel!", CancellationToken).ConfigureAwait(false);

            if (!permissionsResult.Entity.HasPermission(DiscordPermission.SendMessages))
                return await _replyService.RespondWithUserErrorAsync("I don't have permission to send messages in that channel!", CancellationToken).ConfigureAwait(false);

            Embed e = new(title, Description: description ?? default(Optional<string>), Colour: BotConstants.DEFAULT_EMBED_COLOUR);

            Result<IMessage> menuCreationResult = await _channelApi.CreateMessageAsync(channel.ID, embeds: new Embed[] { e }, ct: CancellationToken).ConfigureAwait(false);
            if (!menuCreationResult.IsSuccess)
            {
                await _replyService.RespondWithErrorAsync(CancellationToken).ConfigureAwait(false);
                return menuCreationResult;
            }

            return await _replyService.RespondWithSuccessAsync("Menu created!", CancellationToken).ConfigureAwait(false);
        }
    }
}
