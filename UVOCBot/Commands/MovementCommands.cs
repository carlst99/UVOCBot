using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Rest.Core;
using Remora.Results;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using UVOCBot.Discord.Core;
using UVOCBot.Discord.Core.Abstractions.Services;
using UVOCBot.Discord.Core.Commands;
using UVOCBot.Discord.Core.Commands.Attributes;
using UVOCBot.Discord.Core.Commands.Conditions.Attributes;
using UVOCBot.Discord.Core.Errors;

namespace UVOCBot.Commands;

[RequireContext(ChannelContext.Guild)]
[RequireGuildPermission(DiscordPermission.MoveMembers)]
public class MovementCommands : CommandGroup
{
    private readonly IInteraction _context;
    private readonly IDiscordRestGuildAPI _guildAPI;
    private readonly IVoiceStateCacheService _voiceStateCache;
    private readonly FeedbackService _feedbackService;

    public MovementCommands
    (
        IInteractionContext context,
        IDiscordRestGuildAPI guildAPI,
        IVoiceStateCacheService voiceStateCache,
        FeedbackService feedbackService
    )
    {
        _context = context.Interaction;
        _guildAPI = guildAPI;
        _voiceStateCache = voiceStateCache;
        _feedbackService = feedbackService;
    }

    [Command("move")]
    [Description("Moves members from one voice channel to another")]
    [Deferred]
    public async Task<IResult> MoveCommandAsync
    (
        [Description("The voice channel to move people to")]
        [ChannelTypes(ChannelType.GuildVoice, ChannelType.GuildStageVoice)]
        IChannel moveTo,

        [Description("The voice channel to move people from. Leave empty to move them out of your current channel.")]
        [ChannelTypes(ChannelType.GuildVoice, ChannelType.GuildStageVoice)]
        IChannel? moveFrom = null
    )
    {
        bool isValid = await CheckAndNotifyValidVoiceChannel(moveTo, false).ConfigureAwait(false);
        if (!isValid)
            return Result.FromSuccess();

        Result<Snowflake> moveFromID = await GetMoveFromChannelIdAsync(moveFrom).ConfigureAwait(false);
        if (!moveFromID.IsSuccess)
            return Result.FromSuccess();

        Optional<IReadOnlyList<IVoiceState>> voiceStates = _voiceStateCache.GetChannelVoiceStates(moveFromID.Entity);
        if (!voiceStates.HasValue)
        {
            return await _feedbackService.SendContextualWarningAsync
            (
                "There are no members to move",
                ct: CancellationToken
            ).ConfigureAwait(false);
        }

        foreach (IVoiceState voiceState in voiceStates.Value)
        {
            await _guildAPI.ModifyGuildMemberAsync
            (
                _context.GuildID.Value,
                voiceState.UserID,
                channelID: moveTo.ID,
                ct: CancellationToken
            ).ConfigureAwait(false);
        }

        return await _feedbackService.SendContextualSuccessAsync
        (
            Formatter.Emoji("white_check_mark") + $" Moved {voiceStates.Value.Count} members.",
            ct: CancellationToken
        ).ConfigureAwait(false);
    }

    private async Task<Result<Snowflake>> GetMoveFromChannelIdAsync(IChannel? moveFromChannel = null)
    {
        if (!_context.TryGetUser(out IUser? user))
            return new ArgumentInvalidError("user", "No user was present");

        if (moveFromChannel is not null)
        {
            bool isValid = await CheckAndNotifyValidVoiceChannel(moveFromChannel, true).ConfigureAwait(false);
            if (!isValid)
                return new InvalidOperationError();

            return moveFromChannel.ID;
        }

        Optional<IVoiceState> memberState = _voiceStateCache.GetUserVoiceState(user.ID);
        if (!memberState.HasValue)
        {
            return new GenericCommandError
            (
                "You must be in a voice channel to omit the " + Formatter.InlineQuote("moveFrom") + "parameter."
            );
        }

        return memberState.Value.ChannelID!.Value;
    }

    private async Task<bool> CheckAndNotifyValidVoiceChannel(IChannel channel, bool from)
    {
        if (channel.Type is not (ChannelType.GuildVoice or ChannelType.GuildStageVoice))
        {
            await _feedbackService.SendContextualErrorAsync
            (
                $"Please specify a valid {Formatter.Bold("voice")} channel to move {(from ? "from" : "to")}.",
                ct: CancellationToken
            ).ConfigureAwait(false);

            return false;
        }

        return true;
    }
}
