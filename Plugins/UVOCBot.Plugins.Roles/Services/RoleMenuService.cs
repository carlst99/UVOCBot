using Microsoft.EntityFrameworkCore;
using Remora.Discord.API;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using UVOCBot.Discord.Core.Commands;
using Remora.Rest.Core;
using Remora.Results;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Core;
using UVOCBot.Core.Model;
using UVOCBot.Discord.Core;
using UVOCBot.Plugins.Roles.Abstractions.Services;

namespace UVOCBot.Plugins.Roles.Services;

/// <inheritdoc cref="IRoleMenuService"/>
public class RoleMenuService : IRoleMenuService
{
    private readonly ICommandContext _context;
    private readonly IDiscordRestChannelAPI _channelApi;
    private readonly DiscordContext _dbContext;
    private readonly FeedbackService _feedbackService;

    public RoleMenuService
    (
        ICommandContext context,
        IDiscordRestChannelAPI channelApi,
        DiscordContext dbContext,
        FeedbackService feedbackService
    )
    {
        _context = context;
        _channelApi = channelApi;
        _dbContext = dbContext;
        _feedbackService = feedbackService;
    }

    /// <inheritdoc />
    public bool TryGetGuildRoleMenu(ulong messageID, [NotNullWhen(true)] out GuildRoleMenu? menu)
    {
        menu = null;
        if (!_context.GuildID.HasValue)
            return false;

        menu = _dbContext.RoleMenus
            .Include(grm => grm.Roles)
            .FirstOrDefault
            (
                grm => grm.GuildId == _context.GuildID.Value.Value
                     && grm.MessageId == messageID
            );

        return menu is not null;
    }

    /// <inheritdoc />
    public async Task<Result<IMessage>> CheckRoleMenuMessageExistsAsync(GuildRoleMenu menu, CancellationToken ct = default)
    {
        Result<IMessage> getMessageResult = await _channelApi.GetChannelMessageAsync
        (
            DiscordSnowflake.New(menu.ChannelId),
            DiscordSnowflake.New(menu.MessageId),
            ct
        ).ConfigureAwait(false);

        if (!getMessageResult.IsSuccess)
        {
            await _feedbackService.SendContextualErrorAsync
            (
                "That role menu appears to have been deleted! Please create a new one.",
                ct: ct
            );

            _dbContext.Remove(menu);
            await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        return getMessageResult;
    }

    /// <inheritdoc />
    public async Task<Result<IMessage>> UpdateRoleMenuMessageAsync(GuildRoleMenu menu, CancellationToken ct = default)
        => await _channelApi.EditMessageAsync
        (
            DiscordSnowflake.New(menu.ChannelId),
            DiscordSnowflake.New(menu.MessageId),
            embeds: new[] {
                CreateRoleMenuEmbed(menu)
            },
            components: menu.Roles.Count > 0
                ? CreateRoleMenuMessageComponents(menu)
                : new Optional<IReadOnlyList<IMessageComponent>>(),
            ct: ct
        );

    public IEmbed CreateRoleMenuEmbed(GuildRoleMenu menu)
        => new Embed
        (
            menu.Title,
            Description: menu.Description,
            Colour: DiscordConstants.DEFAULT_EMBED_COLOUR
        );

    private static List<IMessageComponent> CreateRoleMenuMessageComponents(GuildRoleMenu menu)
    {
        List<ButtonComponent> roleButtons = menu.Roles.ConvertAll
        (
            r => new ButtonComponent
            (
                ButtonComponentStyle.Secondary,
                r.Label,
                CustomID: ComponentIDFormatter.GetId(RoleComponentKeys.ToggleRole, r.RoleId.ToString())
            )
        );

        return new List<IMessageComponent>
        (
            roleButtons.Chunk(5)
                .Select(bl => new ActionRowComponent(bl))
        );
    }
}
