using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneOf;
using Remora.Discord.API;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Abstractions.Results;
using Remora.Discord.API.Objects;
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
    private readonly ILogger<RoleMenuService> _logger;
    private readonly IDiscordRestChannelAPI _channelApi;
    private readonly IDiscordRestEmojiAPI _emojiApi;
    private readonly DiscordContext _dbContext;

    public RoleMenuService
    (
        ILogger<RoleMenuService> logger,
        IDiscordRestChannelAPI channelApi,
        IDiscordRestEmojiAPI emojiApi,
        DiscordContext dbContext
    )
    {
        _logger = logger;
        _channelApi = channelApi;
        _emojiApi = emojiApi;
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public bool TryGetGuildRoleMenu
    (
        Optional<Snowflake> guildId,
        ulong messageID,
        [NotNullWhen(true)] out GuildRoleMenu? menu
    )
    {
        menu = null;
        if (!guildId.HasValue)
            return false;

        menu = _dbContext.RoleMenus
            .Include(grm => grm.Roles)
            .FirstOrDefault
            (
                grm => grm.GuildId == guildId.Value.Value
                     && grm.MessageId == messageID
            );

        return menu is not null;
    }

    /// <inheritdoc />
    public async Task<Result<IMessage>> UpdateRoleMenuMessageAsync
    (
        GuildRoleMenu menu,
        bool restoreDeletedMenus,
        CancellationToken ct = default
    )
    {
        menu.Roles.Sort
        (
            (r1, r2) => string.Compare(r1.Label, r2.Label, StringComparison.Ordinal)
        );
        List<IMessageComponent> components = await CreateRoleMenuMessageComponents(menu, ct);

        Result<IMessage> editResult = await _channelApi.EditMessageAsync
        (
            DiscordSnowflake.New(menu.ChannelId),
            DiscordSnowflake.New(menu.MessageId),
            components: components,
            flags: MessageFlags.IsComponentsV2,
            ct: ct
        );

        // If we couldn't edit the message, it's quite possible it was deleted. Let's check if that was the case
        // and attempt to recreate the message if so

        if (editResult.IsSuccess || !restoreDeletedMenus)
            return editResult;

        // Test for an UnknownMessage error - meaning we should recreate the message
        bool failure = editResult.Error is not RestResultError<RestError> restError
            || !restError.Error.Code.TryGet(out DiscordError discordError)
            || discordError is not DiscordError.UnknownMessage;

        if (failure)
        {
            _logger.LogError
            (
                "Failed to edit role menu with guild / channel / message ID {GuildId} / {MessageId} / {ChannelId}",
                menu.GuildId,
                menu.ChannelId,
                menu.MessageId
            );
            return editResult;
        }

        Result<IMessage> createMsgResult = await _channelApi.CreateMessageAsync
        (
            DiscordSnowflake.New(menu.ChannelId),
            components: components,
            flags: MessageFlags.IsComponentsV2,
            ct: ct
        );
        if (createMsgResult.Error is RestResultError<RestError> { Error.Code: { HasValue: true, Value: DiscordError.InvalidFormBody } } createRestError)
        {
            // TODO: Drill all the way down, find IErrorDetails.Code = "COMPONENT_INVALID_EMOJI" or error on IPropertyErrorDetails.Key = "emoji"
            foreach ((string key, OneOf<IPropertyErrorDetails, IReadOnlyList<IErrorDetails>> value) in createRestError.Error.Errors.Value)
            {
            }
        }

        if (createMsgResult.IsDefined(out IMessage? createdMsg))
            menu.MessageId = createdMsg.ID.Value;

        return createMsgResult;
    }

    private async ValueTask<List<IMessageComponent>> CreateRoleMenuMessageComponents
    (
        GuildRoleMenu menu,
        CancellationToken ct
    )
    {
        List<SectionComponent> roleButtonsWithDesc = [];
        List<ButtonComponent> roleButtons = [];

        // Validate emoji, otherwise Discord causes issues
        // foreach (GuildRoleMenuRole role in menu.Roles.Where(x => x.Emoji != null))
        // {
        //     Optional<IPartialEmoji> emoji = Formatter.EmojiFromString(role.Emoji!)
        //         .MapOr(x => new Optional<IPartialEmoji>(x), default);
        //
        //     if (!emoji.HasValue || !emoji.Value.ID.HasValue)
        //         continue;
        //
        //     Result<IEmoji> guildEmoji = await _emojiApi.GetGuildEmojiAsync
        //     (
        //         DiscordSnowflake.New(menu.GuildId),
        //         emoji.Value.ID.Value!.Value, ct
        //     );
        //
        //     if (!guildEmoji.IsSuccess)
        //         role.Emoji = null;
        // }

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

        List<IMessageComponent> containerChildren = [header];
        if (roleButtonsWithDesc.Count > 0)
        {
            containerChildren.Add(new SeparatorComponent());
            containerChildren.AddRange(roleButtonsWithDesc);
        }
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
