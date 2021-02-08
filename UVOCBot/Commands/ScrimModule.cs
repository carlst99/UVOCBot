using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UVOCBot.Model;

namespace UVOCBot.Commands
{
    [Description("Commands pertinent to scrim organisation")]
    [RequireGuild]
    public class ScrimModule : BaseCommandModule
    {
        private readonly List<AccountDistributionRequest> _distributionRequests;

        public ScrimModule()
        {
            _distributionRequests = new List<AccountDistributionRequest>();
        }

        [Command("distribute-accounts")]
        [Aliases("handout", "distribute")]
        [RequireGuild]
        [RequirePermissions(Permissions.SendMessages | Permissions.ManageRoles)]
        public async Task AccountDistributionCommand(
            CommandContext ctx,
            [Description("Accounts will be distributed to people with this role")] DiscordRole role)
        {
            // Try to create a new distribution request
            List<DiscordMember> roleOwners = await ctx.Guild.GetMembersWithRoleAsync(role).ConfigureAwait(false);
            AccountDistributionRequest request = new AccountDistributionRequest(ctx.Member, ctx.Channel, roleOwners);
            if (_distributionRequests.Contains(request))
            {
                await ctx.RespondAsync($"{ctx.Member.Mention}, you already have a pending distribution request! Please check your DMs.").ConfigureAwait(false);
                return;
            }
            _distributionRequests.Add(request);

            // Attempt to open a direct message channel
            DiscordChannel dmChannel;
            try
            {
                dmChannel = await ctx.Member.CreateDmChannelAsync().ConfigureAwait(false);
            }
            catch (UnauthorizedException)
            {
                await ctx.RespondWithDMFailureMessage().ConfigureAwait(false);
                return;
            }

            await dmChannel.TrySendDirectMessage(
                "Send the accounts in your next message. The format should be: ```user1    pass1\r\nuser2    pass2\r\n...```Where there are four spaces between the username and password.",
                ctx).ConfigureAwait(false);
            await ctx.RespondAsync($"{ctx.Member.Mention} please check your DMs! This request will time out in {AccountDistributionRequest.DEFAULT_TIMEOUT_MIN} minutes.").ConfigureAwait(false);

            InteractivityResult<DiscordMessage> accountsMessage = await dmChannel.GetNextMessageAsync(TimeSpan.FromMinutes(AccountDistributionRequest.DEFAULT_TIMEOUT_MIN)).ConfigureAwait(false);
            if (accountsMessage.TimedOut)
            {
                _distributionRequests.Remove(request);
                await dmChannel.TrySendDirectMessage("Your distribution request has timed out.", ctx, $"{ctx.Member.Mention}, your distribution request has timed out.").ConfigureAwait(false);
                return;
            }
        }
    }
}
