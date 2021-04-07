using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Core;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using UVOCBot.Core.Model;
using UVOCBotRemora.Services;

namespace UVOCBotRemora.Commands
{
    [RequireContext(ChannelContext.Guild)]
    [RequireUserGuildPermission(DiscordPermission.MoveMembers)]
    public class MovementCommands : CommandGroup
    {
        private readonly ICommandContext _context;
        private readonly CommandContextReponses _responder;
        private readonly IDiscordRestGuildAPI _guildAPI;
        private readonly IAPIService _dbAPI;
        private readonly IVoiceStateCacheService _voiceStateCache;

        public MovementCommands(ICommandContext context, CommandContextReponses responder, IDiscordRestGuildAPI guildAPI, IAPIService dbAPI, IVoiceStateCacheService voiceStateCache)
        {
            _context = context;
            _responder = responder;
            _guildAPI = guildAPI;
            _dbAPI = dbAPI;
            _voiceStateCache = voiceStateCache;
        }

        [Command("move")]
        [Description("Moves a group from one voice channel to another")]
        public async Task<IResult> MoveCommandAsync(
            [Description("The voice channel to move people to")] IChannel moveTo,
            [Description("An optional voice channel to move people from")] IChannel? moveFrom = null)
        {
            if (moveTo.Type != ChannelType.GuildVoice)
            {
                await _responder.RespondWithErrorAsync(_context, $"Please specify a valid {Formatter.Bold("voice")} channel to move to.", ct: CancellationToken).ConfigureAwait(false);
                return Result.FromSuccess();
            }

            Result<Snowflake> moveFromID = await GetMoveFromChannelIdAsync(moveFrom).ConfigureAwait(false);
            if (!moveFromID.IsSuccess)
                return Result.FromSuccess();

            Optional<IReadOnlyList<IVoiceState>> voiceStates = _voiceStateCache.GetChannelVoiceStates(moveFromID.Entity);
            if (!voiceStates.HasValue)
                return await _responder.RespondWithErrorAsync(_context, "There are no members to move", ct: CancellationToken).ConfigureAwait(false);

            foreach (IVoiceState voiceState in voiceStates.Value)
                await _guildAPI.ModifyGuildMemberAsync(_context.GuildID.Value, voiceState.UserID, channelID: moveTo.ID, ct: CancellationToken).ConfigureAwait(false);

            return await _responder.RespondWithSuccessAsync(_context, Formatter.Emoji("white_check_mark"), ct: CancellationToken).ConfigureAwait(false);
        }

        [Command("move-group")]
        [Description("Moves a group from one voice channel to another")]
        public async Task<IResult> MoveGroupCommandAsync(
            [Description("The member group to move. See the group commands for more info")][DiscordTypeHint(TypeHint.String)] string groupName,
            [Description("The voice channel to move people to")] IChannel moveTo,
            [Description("An optional voice channel to move people from")] IChannel? moveFrom = null)
        {
            if (moveTo.Type != ChannelType.GuildVoice)
            {
                await _responder.RespondWithErrorAsync(_context, $"Please specify a valid {Formatter.Bold("voice")} channel to move to.", ct: CancellationToken).ConfigureAwait(false);
                return Result.FromSuccess();
            }

            Result<Snowflake> moveFromID = await GetMoveFromChannelIdAsync(moveFrom).ConfigureAwait(false);
            if (!moveFromID.IsSuccess)
                return Result.FromSuccess();

            Result<IReadOnlyList<Snowflake>> groupMembers = await GetGroupMembersAsync(groupName).ConfigureAwait(false);
            if (!groupMembers.IsSuccess)
                return Result.FromSuccess(); // Not our error if the group cannot be found

            Optional<IReadOnlyList<IVoiceState>> voiceStates = _voiceStateCache.GetChannelVoiceStates(moveFromID.Entity);
            if (!voiceStates.HasValue)
                return await _responder.RespondWithErrorAsync(_context, "There are no members to move", ct: CancellationToken).ConfigureAwait(false);

            foreach (IVoiceState voiceState in voiceStates.Value)
            {
                if (groupMembers.Entity.Contains(voiceState.UserID))
                    await _guildAPI.ModifyGuildMemberAsync(_context.GuildID.Value, voiceState.UserID, channelID: moveTo.ID, ct: CancellationToken).ConfigureAwait(false);
            }

            return await _responder.RespondWithSuccessAsync(_context, Formatter.Emoji("white_check_mark"), ct: CancellationToken).ConfigureAwait(false);
        }

        private async Task<Result<Snowflake>> GetMoveFromChannelIdAsync(IChannel? moveFromChannel = null)
        {
            if (moveFromChannel is null)
            {
                Optional<IVoiceState> memberState = _voiceStateCache.GetUserVoiceState(_context.User.ID);
                if (!memberState.HasValue)
                {
                    await _responder.RespondWithErrorAsync(_context, "You must be in a voice channel to omit the " + Formatter.InlineQuote("moveFrom") + "parameter.", ct: CancellationToken).ConfigureAwait(false);
                    return Result<Snowflake>.FromError(new InvalidOperationException());
                }

#pragma warning disable CS8629 // Nullable value type may be null.
                return memberState.Value.ChannelID.Value;
#pragma warning restore CS8629 // Nullable value type may be null.
            }
            else
            {
                if (moveFromChannel.Type != ChannelType.GuildVoice)
                {
                    await _responder.RespondWithErrorAsync(_context, $"Please specify a valid {Formatter.Bold("voice")} channel to move from.", ct: CancellationToken).ConfigureAwait(false);
                    return Result<Snowflake>.FromError(new InvalidOperationException());
                }

                return moveFromChannel.ID;
            }
        }

        private async Task<Result<IReadOnlyList<Snowflake>>> GetGroupMembersAsync(string groupName)
        {
            try
            {
                MemberGroupDTO group = await _dbAPI.GetMemberGroup(_context.GuildID.Value.Value, groupName).ConfigureAwait(false);
                return group.UserIds.ConvertAll(i => new Snowflake(i)).AsReadOnly();
            }
            catch (Refit.ValidationApiException vaex) when (vaex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                await _responder.RespondWithErrorAsync(_context, "That group doesn't exist.", ct: CancellationToken).ConfigureAwait(false);
                return Result<IReadOnlyList<Snowflake>>.FromError<ArgumentException>(new ArgumentException("Invalid group name"));
            }
        }
    }
}
