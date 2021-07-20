using FuzzySharp;
using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Core;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Commands;
using UVOCBot.Core.Model;
using UVOCBot.Model.Census;
using UVOCBot.Services.Abstractions;

namespace UVOCBot.Services
{
    public class WelcomeMessageService : IWelcomeMessageService
    {
        private readonly ILogger<WelcomeMessageService> _logger;
        private readonly InteractionContext _context;
        private readonly ICensusApiService _censusApi;
        private readonly IDbApiService _dbApi;
        private readonly IDiscordRestChannelAPI _channelApi;
        private readonly IDiscordRestGuildAPI _guildApi;
        private readonly IReplyService _responder;

        public WelcomeMessageService(
            ILogger<WelcomeMessageService> logger,
            InteractionContext context,
            ICensusApiService censusApi,
            IDbApiService dbApi,
            IDiscordRestChannelAPI channelApi,
            IDiscordRestGuildAPI guildApi,
            IReplyService responder)
        {
            _logger = logger;
            _context = context;
            _censusApi = censusApi;
            _dbApi = dbApi;
            _channelApi = channelApi;
            _guildApi = guildApi;
            _responder = responder;
        }

        public async Task<Result> SendWelcomeMessage(IGuildMemberAdd gatewayEvent, CancellationToken ct = default)
        {
            // Get the welcome message settings
            Result<GuildWelcomeMessageDto> welcomeMessageResult = await GetGuildWelcomeMessage(gatewayEvent.GuildID.Value, ct).ConfigureAwait(false);
            if (!welcomeMessageResult.IsSuccess)
                return Result.FromError(welcomeMessageResult);

            GuildWelcomeMessageDto welcomeMessage = welcomeMessageResult.Entity;
            if (!welcomeMessage.IsEnabled)
                return Result.FromSuccess();

            // Make some nickname guesses
            IEnumerable<string>? nicknameGuesses = null;
            if (welcomeMessage.DoIngameNameGuess)
                nicknameGuesses = await DoFuzzyNicknameGuess(_context.User.Username, welcomeMessage.OutfitId, ct).ConfigureAwait(false);

            // Prepare components of the welcome message
            List<ButtonComponent> messageButtons = CreateWelcomeMessageButtons(welcomeMessage, nicknameGuesses, _context.User.ID.Value);
            string messageContent = SubstituteMessageVariables(welcomeMessage.Message);

            // Send the welcome message
            Result<IMessage> sendWelcomeMessageResult = await _channelApi.CreateMessageAsync(
                new Snowflake(welcomeMessage.ChannelId),
                messageContent,
                allowedMentions: new AllowedMentions(new List<MentionType>() { MentionType.Users }),
                components: new List<IMessageComponent>() { new ActionRowComponent(messageButtons) },
                ct: ct).ConfigureAwait(false);

            // Assign default roles
            await ModifyRoles(
                gatewayEvent.GuildID,
                _context.User.ID,
                gatewayEvent.Roles.ToList(),
                rolesToAdd: welcomeMessage.DefaultRoles,
                ct: ct).ConfigureAwait(false);

            return sendWelcomeMessageResult.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(sendWelcomeMessageResult);
        }

        private async Task<Result<GuildWelcomeMessageDto>> GetGuildWelcomeMessage(ulong guildId, CancellationToken ct = default)
        {
            Result<GuildWelcomeMessageDto> welcomeMessageResult = await _dbApi.GetGuildWelcomeMessageAsync(guildId, ct).ConfigureAwait(false);
            if (!welcomeMessageResult.IsSuccess)
                _logger.LogError("Failed to retrieve GuildWelcomeMessage object: {error}", welcomeMessageResult.Error);

            return welcomeMessageResult;
        }

        #region Message composition

        private static List<ButtonComponent> CreateWelcomeMessageButtons(GuildWelcomeMessageDto welcomeMessage, IEnumerable<string>? nicknameGuesses, ulong userId)
        {
            List<ButtonComponent> messageButtons = new();

            if (welcomeMessage.AlternateRoles.Count > 0 && !string.IsNullOrEmpty(welcomeMessage.AlternateRoleLabel))
            {
                messageButtons.Add(new ButtonComponent(
                    ButtonComponentStyle.Danger,
                    welcomeMessage.AlternateRoleLabel,
                    CustomID: ComponentIdFormatter.GetId(ComponentAction.WelcomeMessageSetAlternate, userId.ToString())));
            }

            if (nicknameGuesses is not null)
            {
                foreach (string nickname in nicknameGuesses)
                {
                    messageButtons.Add(new ButtonComponent(
                        ButtonComponentStyle.Primary,
                        "My PS2 name is: " + nickname,
                        CustomID: ComponentIdFormatter.GetId(ComponentAction.WelcomeMessageNicknameGuess, userId.ToString() + '@' + nickname)));
                }

                messageButtons.Add(new ButtonComponent(
                    ButtonComponentStyle.Secondary,
                    "My PS2 name is none of these!",
                    CustomID: ComponentIdFormatter.GetId(ComponentAction.WelcomeMessageNicknameNoMatch, userId.ToString())));
            }

            return messageButtons;
        }

        private string SubstituteMessageVariables(string welcomeMessage)
        {
            return welcomeMessage.Replace("<name>", Formatter.UserMention(_context.User.ID));
        }

        #endregion

        #region Nicknames

        public async Task<Result> SetNicknameFromGuess(IInteractionCreate gatewayEvent, CancellationToken ct = default)
        {
            if (!gatewayEvent.GuildID.HasValue)
                return Result.FromSuccess();

            if (gatewayEvent.Data.Value is null || gatewayEvent.Data.Value.CustomID.Value is null)
                return Result.FromSuccess();

            ComponentIdFormatter.Parse(gatewayEvent.Data.Value.CustomID.Value, out ComponentAction _, out string payload);
            string[] payloadComponents = payload.Split('@');
            ulong userId = ulong.Parse(payloadComponents[0]);

            // Check that the user who clicked the button is the focus of the welcome message
            if (_context.User.ID.Value != userId)
            {
                await _responder.RespondWithSuccessAsync("Hold it, bud. You can't do that!", ct, new AllowedMentions()).ConfigureAwait(false);
                return Result.FromSuccess();
            }

            await _guildApi.ModifyGuildMemberAsync(gatewayEvent.GuildID.Value, _context.User.ID, payloadComponents[1], ct: ct).ConfigureAwait(false);

            Result<IMessage> alertResponse = await _responder.RespondWithSuccessAsync(
                $"Your nickname has been updated to { Formatter.Bold(payloadComponents[1]) }!",
                ct).ConfigureAwait(false);

            return alertResponse.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(alertResponse);
        }

        public async Task<Result> InformNicknameNoMatch(IInteractionCreate gatewayEvent, CancellationToken ct = default)
        {
            Result<IMessage> alertResponse = await _responder.RespondWithSuccessAsync(
                "Please set your nickname to the name of your PlanetSide 2 character!",
                ct).ConfigureAwait(false);

            return alertResponse.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(alertResponse);
        }

        private async Task<IEnumerable<string>> DoFuzzyNicknameGuess(string username, ulong outfitId, CancellationToken ct = default)
        {
            const int minMatchRatio = 65;
            const int maxGuesses = 2;
            List<Tuple<string, int>> nicknameGuesses = new();

            try
            {
                List<NewOutfitMember> newMembers = await _censusApi.GetNewOutfitMembersAsync(outfitId, 10, ct).ConfigureAwait(false);
                newMembers.Sort((NewOutfitMember x, NewOutfitMember y) => y.MemberSince.CompareTo(x.MemberSince));

                for (int i = 0; i < newMembers.Count; i++)
                {
                    NewOutfitMember m = newMembers[i];

                    int matchRatio = Fuzz.PartialRatio(m.CharacterName.Name.First, username);
                    if (matchRatio > minMatchRatio)
                    {
                        nicknameGuesses.Add(new Tuple<string, int>(m.CharacterName.Name.First, matchRatio));
                        newMembers.RemoveAt(i);
                        i--;
                    }
                }

                nicknameGuesses.Sort((Tuple<string, int> x, Tuple<string, int> y) => y.Item2.CompareTo(x.Item2));

                if (nicknameGuesses.Count < maxGuesses)
                    nicknameGuesses.AddRange(newMembers.Take(maxGuesses - nicknameGuesses.Count).Select(m => new Tuple<string, int>(m.CharacterName.Name.First, 0)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to make a nickname guess.");
            }

            return nicknameGuesses.Take(2).Select(g => g.Item1);
        }

        #endregion

        #region Roles

        /// <summary>
        /// Assigns the alternate roles of the <see cref="GuildWelcomeMessageDto"/> to the member, and removes the default roles.
        /// </summary>
        /// <param name="gatewayEvent">The interaction event to perform this operation on.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> used to stop the operation.</param>
        /// <returns>A <see cref="Result"/> indicating if the operation was successful.</returns>
        public async Task<Result> SetAlternateRoles(IInteractionCreate gatewayEvent, CancellationToken ct = default)
        {
            if (!gatewayEvent.GuildID.HasValue)
                return Result.FromSuccess();

            if (gatewayEvent.Data.Value is null || gatewayEvent.Data.Value.CustomID.Value is null)
                return Result.FromSuccess();

            // Get the welcome message settings
            Result<GuildWelcomeMessageDto> welcomeMessage = await GetGuildWelcomeMessage(gatewayEvent.GuildID.Value.Value, ct).ConfigureAwait(false);
            if (!welcomeMessage.IsSuccess)
                return Result.FromError(welcomeMessage);

            ComponentIdFormatter.Parse(gatewayEvent.Data.Value.CustomID.Value, out ComponentAction _, out string payload);
            ulong userId = ulong.Parse(payload);

            // Check that the user who clicked the button is the focus of the welcome message
            if (_context.User.ID.Value != userId)
            {
                await _responder.RespondWithSuccessAsync("Hold it, bud. You can't do that!", ct, new AllowedMentions()).ConfigureAwait(false);
                return Result.FromSuccess();
            }

            // Remove the default roles and add the alternate roles
            Result roleChangeResult = await ModifyRoles(
                gatewayEvent.GuildID.Value,
                _context.User.ID,
                gatewayEvent.Member.Value?.Roles.ToList(),
                welcomeMessage.Entity.AlternateRoles,
                welcomeMessage.Entity.DefaultRoles,
                ct).ConfigureAwait(false);

            if (!roleChangeResult.IsSuccess)
            {
                _logger.LogError("Failed to modify member roles: {error}", roleChangeResult.Error);
                return roleChangeResult;
            }

            // Inform the user of their role change
            string rolesStringList = string.Join(' ', welcomeMessage.Entity.AlternateRoles.Select(r => Formatter.RoleMention(r)));
            Result<IMessage> alertResponse = await _responder.RespondWithSuccessAsync(
                $"You've been given the following roles: { rolesStringList }",
                ct,
                new AllowedMentions()).ConfigureAwait(false);

            return alertResponse.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(alertResponse);
        }

        /// <summary>
        /// Modifies the roles of a guild member.
        /// </summary>
        /// <param name="guildId">The guild that the member is part of.</param>
        /// <param name="userId">The user to assign the roles to.</param>
        /// <param name="currentRoles">The user's existing roles. Optional.</param>
        /// <param name="rolesToAdd">The roles to add.</param>
        /// <param name="rolesToAdd">The roles to remove.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> used to stop the operation.</param>
        /// <returns>A <see cref="Result"/> indicating if the operation was successful.</returns>
        private async Task<Result> ModifyRoles(Snowflake guildId, Snowflake userId, List<Snowflake>? currentRoles, IEnumerable<ulong>? rolesToAdd = null, IEnumerable<ulong>? rolesToRemove = null, CancellationToken ct = default)
        {
            if (currentRoles is null)
            {
                Result<IGuildMember> memberResult = await _guildApi.GetGuildMemberAsync(guildId, userId, ct).ConfigureAwait(false);
                if (!memberResult.IsSuccess)
                    return Result.FromError(memberResult);
                else
                    currentRoles = memberResult.Entity.Roles.ToList();
            }

            if (rolesToAdd is not null)
            {
                foreach (ulong roleId in rolesToAdd)
                {
                    Snowflake snowflake = new(roleId);
                    if (!currentRoles.Contains(snowflake))
                        currentRoles.Add(snowflake);
                }
            }

            if (rolesToRemove is not null)
            {
                foreach (ulong roleId in rolesToRemove)
                {
                    Snowflake snowflake = new(roleId);
                    if (currentRoles.Contains(snowflake))
                        currentRoles.Remove(snowflake);
                }
            }

            return await _guildApi.ModifyGuildMemberAsync(guildId, userId, roles: currentRoles, ct: ct).ConfigureAwait(false);
        }

        #endregion
    }
}
