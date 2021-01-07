using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using System;
using System.Threading.Tasks;

namespace UVOCBot.Commands
{
    [Group("notify")]
    [Aliases("notifications", "notification", "remind", "reminder")]
    [Description("Commands that let a guild schedule one-off/recurring reminders")]
    [RequireGuild]
    public sealed class NotificationModule : BaseCommandModule
    {
        private const string CANCEL_WORD = Program.PREFIX + "cancel";

        private readonly IArgumentConverter<DiscordRole> _roleConverter = new DiscordRoleConverter();
        private readonly IArgumentConverter<DiscordChannel> _channelConverter = new DiscordChannelConverter();
        private readonly IArgumentConverter<DateTimeOffset> _dateTimeOffsetConverter = new DateTimeOffsetConverter();

        [Command("schedule")]
        [Description("Lets the sender schedule a one-off or recurring notification")]
        public async Task ScheduleCommand(CommandContext ctx)
        {
            Optional<DateTimeOffset> sendTimeTest = await GetValueFromMessageWithReattemptsAsync(
                ctx, _dateTimeOffsetConverter, "What date and time would you like to send the message at? (MM/DD/YYYY HH:mm:ss)", "Could not parse that date, please use the format MM/DD/YYYY HH:mm:ss").ConfigureAwait(false);

            await ctx.RespondAsync(sendTimeTest.Value.ToString()).ConfigureAwait(false);
            return;

            await ctx.RespondAsync($"{Program.NAME} will now walk you through creating a scheduled notification. If you would like to cancel this process at any stage, type {CANCEL_WORD}").ConfigureAwait(false);

            Optional<DiscordChannel> channel = await GetValueFromMessageWithReattemptsAsync(
                ctx, _channelConverter, "Which channel would you like the notification to be posted in?", "That channel does not exist or you cannot access it. Please try again").ConfigureAwait(false);
            if (!channel.HasValue)
                return;

            Optional<DiscordRole> role = await GetValueFromMessageWithReattemptsAsync(
                ctx, _roleConverter, "Which role would you like to notify?", "That role does not exist. Please try again").ConfigureAwait(false);
            if (!role.HasValue)
                return;

            if (!await PerformMentionChecks(ctx, role.Value, channel.Value).ConfigureAwait(false))
                return;

            // Give the user five minutes here, as typing a message can take a while
            await ctx.RespondAsync("What would you like the notification to say?").ConfigureAwait(false);
            InteractivityResult<DiscordMessage> notificationContent = await ctx.Message.GetNextMessageAsync(TimeSpan.FromMinutes(5)).ConfigureAwait(false);
            if (notificationContent.TimedOut)
                return;

            Optional<DateTimeOffset> sendTime = await GetValueFromMessageWithReattemptsAsync(
                ctx, _dateTimeOffsetConverter, "What date and time would you like to send the message at? (MM/DD/YYYY HH:mm:ss)", "Could not parse that date, please use the format MM/DD/YYYY HH:mm:ss").ConfigureAwait(false);

            string message = "Great! Your notification (ID: {id}) has been scheduled for {DD @ HH:MM}. " +
                $"Users with the role `{role.Value.Name}` will be notified in {channel.Value.Mention} with the message:";

            await ctx.RespondAsync(message).ConfigureAwait(false);
            await ctx.RespondAsync(notificationContent.Result.Content).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets a value from a user message, allowing them to retry an infinite number of times
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ctx"></param>
        /// <param name="converter">The argument converter that should be used to extract the value from the user message</param>
        /// <param name="initialMessage">The initial message to send, to ask the user for a certain value. This message is only sent once</param>
        /// <param name="onFailureMessage">The message to send when a user fails to provide a correct value</param>
        /// <returns></returns>
        private async Task<Optional<T>> GetValueFromMessageWithReattemptsAsync<T>(CommandContext ctx, IArgumentConverter<T> converter, string initialMessage, string onFailureMessage)
        {
            await ctx.RespondAsync(initialMessage).ConfigureAwait(false);

            while (true)
            {
                InteractivityResult<DiscordMessage> result = await ctx.Message.GetNextMessageAsync().ConfigureAwait(false);

                if (!result.TimedOut)
                {
                    // Return if the user cancels
                    if (result.Result.Content.Equals(CANCEL_WORD, StringComparison.OrdinalIgnoreCase))
                        return Optional.FromNoValue<T>();

                    Optional<T> role = await converter.ConvertAsync(result.Result.Content, ctx).ConfigureAwait(false);
                    if (!role.HasValue)
                        await ctx.RespondAsync(onFailureMessage).ConfigureAwait(false);
                    else
                        return role;
                }
                else
                {
                    return Optional.FromNoValue<T>();
                }
            }
        }

        /// <summary>
        /// Checks that the sender and UVOCBot are able to mention the provided role in the specified channel. Notifies the sender appropriately if it cannot
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="role">The role to be mentioned by UVOCBot</param>
        /// <param name="channel">The channel in which the role will be mentioned</param>
        /// <returns></returns>
        private async Task<bool> PerformMentionChecks(CommandContext ctx, DiscordRole role, DiscordChannel channel)
        {
            if (!role.IsMentionable)
            {
                Permissions senderPerms = ctx.Member.PermissionsIn(channel);
                if ((senderPerms & Permissions.MentionEveryone) == 0)
                {
                    await ctx.RespondAsync($"You do not have permission to ping the `{role.Name}` role in {channel.Mention}").ConfigureAwait(false);
                    return false;
                }
                else if ((ctx.Guild.Permissions & Permissions.MentionEveryone) == 0)
                {
                    await ctx.RespondAsync($"{Program.NAME} does not have permission to ping the `{role.Name}` role in {channel.Mention}").ConfigureAwait(false);
                    return false;
                }
            }

            return true;
        }
    }
}
