using Microsoft.EntityFrameworkCore;
using Remora.Discord.API;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Abstractions.Results;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using UVOCBot.Discord.Core.Commands;
using Remora.Rest.Core;
using Remora.Rest.Results;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
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
    private readonly IInteraction _context;
    private readonly IDiscordRestChannelAPI _channelApi;
    private readonly DiscordContext _dbContext;
    private readonly FeedbackService _feedbackService;

    public RoleMenuService
    (
        IInteractionContext context,
        IDiscordRestChannelAPI channelApi,
        DiscordContext dbContext,
        FeedbackService feedbackService
    )
    {
        _context = context.Interaction;
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
    {
        menu.Roles.Sort
        (
            (r1, r2) => string.Compare(r1.Label, r2.Label, StringComparison.Ordinal)
        );

        Result<IMessage> editResult = await _channelApi.EditMessageAsync
        (
            DiscordSnowflake.New(menu.ChannelId),
            DiscordSnowflake.New(menu.MessageId),
            components: CreateRoleMenuMessageComponents(menu),
            flags: MessageFlags.IsComponentsV2,
            ct: ct
        );

        // If we couldn't edit the message, it's quite possible it was deleted. Let's check if that was the case,
        // and attempt to recreate the message if so

        if (editResult.IsSuccess)
            return editResult;

        if (editResult.Error is not RestResultError<RestError> restError)
            return editResult;

        if (!restError.Error.Code.TryGet(out DiscordError discordError))
            return editResult;

        if (discordError is not DiscordError.UnknownMessage)
            return editResult;

        return await _channelApi.CreateMessageAsync
        (
            DiscordSnowflake.New(menu.ChannelId),
            components: CreateRoleMenuMessageComponents(menu),
            flags: MessageFlags.IsComponentsV2,
            ct: ct
        );
    }

    private static List<IMessageComponent> CreateRoleMenuMessageComponents(GuildRoleMenu menu)
    {
        List<SectionComponent> roleButtonsWithDesc = [];
        List<ButtonComponent> roleButtons = [];

        foreach (GuildRoleMenuRole role in menu.Roles.Where(x => string.IsNullOrEmpty(x.Description)))
        {
            Optional<IPartialEmoji> emoji = default;
            if (role.Emoji is not null)
                emoji = Formatter.EmojiFromString(role.Emoji).MapOr(x => new Optional<IPartialEmoji>(x), default);

            roleButtons.Add(new ButtonComponent
            (
                ButtonComponentStyle.Secondary,
                role.Label,
                emoji,
                ComponentIDFormatter.GetId(RoleComponentKeys.ToggleRole, role.RoleId.ToString())
            ));
        }

        foreach (GuildRoleMenuRole role in menu.Roles.Where(x => !string.IsNullOrEmpty(x.Description)))
        {
            Optional<IPartialEmoji> emoji = default;
            if (role.Emoji is not null)
                emoji = Formatter.EmojiFromString(role.Emoji).MapOr(x => new Optional<IPartialEmoji>(x), default);

            roleButtonsWithDesc.Add(new SectionComponent
            (
                [new TextDisplayComponent(role.Description!)],
                new ButtonComponent
                (
                    ButtonComponentStyle.Secondary,
                    role.Label,
                    emoji,
                    ComponentIDFormatter.GetId(RoleComponentKeys.ToggleRole, role.RoleId.ToString())
                )
            ));
        }

        StringBuilder headerSb = new(Formatter.Header2(menu.Title));
        if (!string.IsNullOrEmpty(menu.Description))
            headerSb.AppendLine().Append(menu.Description);
        TextDisplayComponent header = new(headerSb.ToString());

        List<ActionRowComponent> actionRows = roleButtons.Chunk(5)
            .Select(bl => new ActionRowComponent(bl))
            .ToList();

        // Only add the second separator if we have action rows. No need to worry about the first separator, as
        // if there are no role buttons with a description, it'll act as the separator for the action rows.
        // And if there's no roles whatsoever, we don't care how it looks, because it isn't a proper role menu!
        List<IMessageComponent> containerChildren = [header, new SeparatorComponent(), ..roleButtonsWithDesc];
        if (actionRows.Count > 0)
        {
            containerChildren.Add(new SeparatorComponent());
            containerChildren.AddRange(actionRows);
        }

        ContainerComponent container = new
        (
            containerChildren,
            false,
            DiscordConstants.DEFAULT_EMBED_COLOUR
        );

        return [container];
    }
}
