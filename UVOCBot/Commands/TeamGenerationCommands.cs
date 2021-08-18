using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UVOCBot.Commands.Conditions.Attributes;
using UVOCBot.Core.Dto;
using UVOCBot.Services.Abstractions;

namespace UVOCBot.Commands
{
    [Group("team")]
    [Description("Commands that help with team generation")]
    [RequireContext(ChannelContext.Guild)]
    public class TeamGenerationCommands : CommandGroup
    {
        private readonly ICommandContext _context;
        private readonly IReplyService _responder;
        private readonly IDiscordRestGuildAPI _guildAPI;
        private readonly IDbApiService _dbAPI;

        public TeamGenerationCommands(ICommandContext context, IReplyService responder, IDiscordRestGuildAPI guildAPI, IDbApiService dbAPI)
        {
            _context = context;
            _responder = responder;
            _guildAPI = guildAPI;
            _dbAPI = dbAPI;
        }

        [Command("random-from-role")]
        [Description("Randomly sorts members with a certain role into teams")]
        public async Task<IResult> RandomFromRoleCommandAsync(
            [Description("Teams will be generated using members with this role")] IRole role,
            [Description("The number of teams to generate")] int numberOfTeams)
        {
            if (numberOfTeams < 2)
                return await _responder.RespondWithUserErrorAsync("At least two teams are required", CancellationToken).ConfigureAwait(false);

            List<ulong> roleMembers = new();

            await foreach (Result<IReadOnlyList<IGuildMember>> users in _guildAPI.GetAllMembersAsync(_context.GuildID.Value, (m) => m.Roles.Contains(role.ID), CancellationToken))
            {
                if (!users.IsSuccess || users.Entity is null)
                {
                    await _responder.RespondWithErrorAsync(CancellationToken).ConfigureAwait(false);
                    return Result.FromError(users);
                }

                roleMembers.AddRange(users.Entity.Select(m => m.User.Value.ID.Value));
            }

            return await SendRandomTeams(roleMembers, numberOfTeams, $"Build from the role {Formatter.RoleMention(role.ID)}").ConfigureAwait(false);
        }

        [Command("random-from-group")]
        [Description("Randomly sorts members of a group into teams")]
        public async Task<IResult> RandomFromGroupCommandAsync(
            [Description("Teams will be generated using members of this group")] string groupName,
            [Description("The number of teams to generate")] int numberOfTeams)
        {
            if (numberOfTeams < 2)
                return await _responder.RespondWithUserErrorAsync("At least two teams are required", CancellationToken).ConfigureAwait(false);

            Result<MemberGroupDto> group = await _dbAPI.GetMemberGroupAsync(_context.GuildID.Value.Value, groupName, CancellationToken).ConfigureAwait(false);
            if (!group.IsSuccess)
            {
                if (group.Error is Model.HttpStatusCodeError er && er.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return await _responder.RespondWithUserErrorAsync("That group doesn't exist.", CancellationToken).ConfigureAwait(false);
                else
                    return await _responder.RespondWithErrorAsync(CancellationToken).ConfigureAwait(false);
            }

            return await SendRandomTeams(group.Entity.UserIds, numberOfTeams, $"Built from the group {Formatter.InlineQuote(group.Entity.GroupName)}").ConfigureAwait(false);
        }

        private async Task<IResult> SendRandomTeams(IList<ulong> memberPool, int teamCount, string embedDescription)
        {
            if (memberPool.Count < teamCount)
                return await _responder.RespondWithUserErrorAsync("There cannot be more teams than team members", CancellationToken).ConfigureAwait(false);

            List<List<ulong>> teams = await CreateRandomTeams(memberPool, teamCount).ConfigureAwait(false);

            return await _responder.RespondWithEmbedAsync(
                BuildTeamsEmbed(teams, "Random Teams", embedDescription),
                CancellationToken,
                allowedMentions: new AllowedMentions()).ConfigureAwait(false);
        }

        private async Task<List<List<ulong>>> CreateRandomTeams(IList<ulong> memberPool, int teamCount)
        {
            List<List<ulong>> teams = new(teamCount);

            await Task.Run(() =>
            {
                memberPool.Shuffle();

                for (int i = 0; i < teamCount; i++)
                    teams.Add(new List<ulong>());

                int teamPos = 0;
                for (int i = 0; i < memberPool.Count; i++)
                {
                    teams[teamPos++].Add(memberPool[i]);
                    if (teamPos == teamCount)
                        teamPos = 0;
                }
            }, CancellationToken).ConfigureAwait(false);

            return teams;
        }

        private static Embed BuildTeamsEmbed(List<List<ulong>> teams, string embedTitle, string embedDescription)
        {
            // TODO: Add support for teams that will exceed longer than 6000 characters. See #22
            List<EmbedField> embedFields = new();

            for (int i = 0; i < teams.Count; i++)
            {
                StringBuilder sb = new();

                foreach (ulong userID in teams[i])
                    sb.Append("- ").AppendLine(Formatter.UserMention(userID));

                embedFields.Add(new EmbedField($"Team {i + 1}", sb.ToString()));
            }

            return new Embed
            {
                Colour = BotConstants.DEFAULT_EMBED_COLOUR,
                Title = embedTitle,
                Description = embedDescription,
                Fields = embedFields
            };
        }
    }
}
