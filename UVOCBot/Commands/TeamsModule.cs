using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UVOCBot.Exceptions;
using UVOCBot.Extensions;

namespace UVOCBot.Commands
{
    [Group("teams")]
    [Aliases("team")]
    [Description("Commands that help with generating team lists")]
    [RequireGuild]
    public class TeamsModule : BaseCommandModule
    {
        [Command("random-teams")]
        [Aliases("rt", "random")]
        [Description("Generates any number of random teams from members with a particular role")]
        public async Task RandomTeamsCommand(
            CommandContext ctx,
            [Description("Anyone with this role will be randomised into teams")] DiscordRole randomiseMembersOf,
            [Description("These people will be the team captains")] params DiscordMember[] captains)
        {
            await ctx.TriggerTypingAsync().ConfigureAwait(false);
            List<DiscordMember> roleMembers = await GetMembersWithRoleAsync(ctx.Guild, randomiseMembersOf).ConfigureAwait(false);

            foreach (DiscordMember c in captains)
            {
                if (roleMembers.Contains(c))
                    roleMembers.Remove(c);
            }

            List<DiscordMember> teamCaptains;
            try
            {
                teamCaptains = await CheckForDuplicateMembers(captains).ConfigureAwait(false);
            } catch (DuplicateItemException<DiscordMember> diex)
            {
                await ctx.RespondAsync("There cannot be duplicate captains - " + diex.DuplicateItem.DisplayName).ConfigureAwait(false);
                return;
            }

            await SendRandomTeams(ctx, roleMembers, teamCaptains.Count, "Random Teams", teamCaptains).ConfigureAwait(false);
        }

        [Command("random-teams")]
        public async Task RandomTeamsCommand(
            CommandContext ctx,
            [Description("Anyone with this role will be randomised into teams")] DiscordRole randomiseMembersOf,
            [Description("The number of teams to generate")] int numberOfTeams)
        {
            await ctx.TriggerTypingAsync().ConfigureAwait(false);
            List<DiscordMember> roleMembers = await GetMembersWithRoleAsync(ctx.Guild, randomiseMembersOf).ConfigureAwait(false);

            if (roleMembers.Count < numberOfTeams)
            {
                await ctx.RespondAsync("There cannot be more teams than team members").ConfigureAwait(false);
                return;
            }

            await SendRandomTeams(ctx, roleMembers, numberOfTeams, "Random Teams").ConfigureAwait(false);
        }

        [Command("random-teams")]
        [Description("Generates any number of random teams from the given members")]
        public async Task RandomTeamsCommand(
            CommandContext ctx,
            [Description("The number of teams to generate")] int numberOfTeams,
            [Description("True if the first members listed should be designated as team captains")] bool firstListedMembersAsCaptains,
            [Description("The members from whom to form teams")] params DiscordMember[] teamMembers)
        {
            await ctx.TriggerTypingAsync().ConfigureAwait(false);

            if (numberOfTeams > teamMembers.Length)
            {
                await ctx.RespondAsync("There cannot be more teams than team members").ConfigureAwait(false);
                return;
            }

            List<DiscordMember> memberPool;
            try
            {
                memberPool = await CheckForDuplicateMembers(teamMembers).ConfigureAwait(false);
            }
            catch (DuplicateItemException<DiscordMember> diex)
            {
                await ctx.RespondAsync("There cannot be duplicate members - " + diex.DuplicateItem.DisplayName).ConfigureAwait(false);
                return;
            }

            List<DiscordMember> teamCaptains = null;
            if (firstListedMembersAsCaptains)
            {
                teamCaptains = memberPool.Take(numberOfTeams).ToList();
                memberPool.RemoveRange(0, numberOfTeams);
            }

            await SendRandomTeams(ctx, memberPool, numberOfTeams, "Random Teams", teamCaptains).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets all the members in a guild who have a particular role
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="role"></param>
        /// <returns></returns>
        private async Task<List<DiscordMember>> GetMembersWithRoleAsync(DiscordGuild guild, DiscordRole role)
        {
            IReadOnlyCollection<DiscordMember> allMembers = await guild.GetAllMembersAsync().ConfigureAwait(false);
            return allMembers.Where(m => m.Roles.Contains(role)).ToList();
        }

        /// <summary>
        /// Checks that there are no duplicated members in a list
        /// </summary>
        /// <param name="captains"></param>
        /// <returns></returns>
        /// <exception cref="DuplicateItemException{T}">Thrown if a duplicate member is detected. The <see cref="DuplicateItemException{T}.DuplicateItem"/> property is set to the member that was duplicated</exception>
        private async Task<List<DiscordMember>> CheckForDuplicateMembers(IEnumerable<DiscordMember> captains)
        {
            List<DiscordMember> teamCaptains = new List<DiscordMember>();

            await Task.Run(() =>
            {
                foreach (DiscordMember element in captains)
                {
                    if (teamCaptains.Contains(element))
                        throw new DuplicateItemException<DiscordMember>(element);
                    else
                        teamCaptains.Add(element);
                }
            }).ConfigureAwait(false);

            return teamCaptains;
        }

        private async Task<List<List<DiscordMember>>> CreateRandomTeams(IList<DiscordMember> memberPool, int teamCount = 2)
        {
            if (teamCount < 1)
                throw new ArgumentOutOfRangeException(nameof(teamCount), "At least one team is required");

            List<List<DiscordMember>> teams = new List<List<DiscordMember>>(teamCount);

            await Task.Run(() =>
            {
                memberPool.Shuffle();

                for (int i = 0; i < teamCount; i++)
                    teams.Add(new List<DiscordMember>());

                int teamPos = 0;
                for (int i = 0; i < memberPool.Count; i++)
                {
                    teams[teamPos++].Add(memberPool[i]);
                    if (teamPos == teamCount)
                        teamPos = 0;
                }
            }).ConfigureAwait(false);

            return teams;
        }

        private DiscordEmbed BuildTeamsEmbed(List<List<DiscordMember>> teams, IList<DiscordMember> captains, string embedTitle)
        {
            // TODO: Add support for teams that will exceed longer than 6000 characters

            if (captains is not null && teams.Count != captains.Count)
                throw new ArgumentOutOfRangeException(nameof(captains), "There must be the same number of captains as team members");

            DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
            {
                Color = DiscordColor.Aquamarine,
                Timestamp = DateTimeOffset.UtcNow,
                Title = embedTitle
            };

            for (int i = 0; i < teams.Count; i++)
            {
                StringBuilder sb = new StringBuilder();

                if (teams[i].Count != 0)
                {
                    foreach (DiscordMember m in teams[i])
                    sb.Append("- ").AppendLine(m.DisplayName);
                } else
                {
                    sb.Append("- No Members");
                }

                string teamTitle = $"Team {i + 1}";
                if (captains is not null)
                    teamTitle += $" - Captain {captains[i].DisplayName}";

                builder.AddField(teamTitle, sb.ToString());
            }

            return builder.Build();
        }

        /// <summary>
        /// Creates a replies with a random team
        /// </summary>
        /// <param name="ctx">The context to reply to</param>
        /// <param name="memberPool">The members to build the teams from</param>
        /// <param name="teamCount">The number of teams to create</param>
        /// <param name="embedName">The name of the teams embed</param>
        /// <param name="teamCaptains">Team captains, if applicable</param>
        /// <returns></returns>
        private async Task SendRandomTeams(CommandContext ctx, IList<DiscordMember> memberPool, int teamCount, string embedName, IList<DiscordMember> teamCaptains = null)
        {
            List<List<DiscordMember>> teams = await CreateRandomTeams(memberPool, teamCount).ConfigureAwait(false);

            try
            {
                DiscordEmbed embed = BuildTeamsEmbed(teams, teamCaptains, embedName);
                await ctx.RespondAsync(embed: embed).ConfigureAwait(false);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                await ctx.RespondAsync(ex.Message).ConfigureAwait(false);
                throw;
            }
        }
    }
}
