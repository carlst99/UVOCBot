using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using System;
using System.Threading.Tasks;
using UVOCBot.Core.Model;
using UVOCBot.Extensions;

namespace UVOCBot.Commands
{
    [Group("notify")]
    [Aliases("notifications", "notification", "remind", "reminder")]
    [Description("Commands that let a guild schedule one-off/recurring reminders")]
    [RequireGuild]
    public sealed class NotificationModule : BaseCommandModule
    {
        private const string CANCEL_WORD = Program.PREFIX + "cancel";
        private const string DATETIME_FORMAT = "dd/MM/yyyy HH:mm";
        private const string DATETIME_FORMAT_EXAMPLE = "02/11/2032 18:30";

        private readonly IArgumentConverter<DiscordRole> _roleConverter = new DiscordRoleConverter();
        private readonly IArgumentConverter<DiscordChannel> _channelConverter = new DiscordChannelConverter();
        private readonly IArgumentConverter<DateTimeOffset> _dateTimeOffsetConverter = new CustomDateTimeOffsetConverter(DATETIME_FORMAT);
        private readonly IArgumentConverter<uint> _uintConverter = new Uint32Converter();

        [Command("schedule")]
        [Description("Lets the sender schedule a one-off or recurring notification")]
        public async Task ScheduleCommand(CommandContext ctx)
        {
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

            // Get repeats and repeat count
            Optional<Tuple<NotificationRepeatPeriod, uint>> repeat = await GetRepeat(ctx).ConfigureAwait(false);
            if (!repeat.HasValue)
                return;

            Optional<DateTimeOffset> sendTime = await GetTime(ctx).ConfigureAwait(false);

            string message = $"Great! Your notification (ID: `{{id}}`) has been scheduled for `{sendTime.Value.ToString(DATETIME_FORMAT)} UTC`. " +
                $"It will repeat `{repeat.Value.Item1}`";

            if (repeat.Value.Item2 != 0)
                message += $" for `{repeat.Value.Item2}` times. ";
            else
                message += ". ";

            message += $"Users with the role `{role.Value.Name}` will be notified in {channel.Value.Mention} with the message:";

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
        private async Task<Optional<T>> GetValueFromMessageWithReattemptsAsync<T>(CommandContext ctx, IArgumentConverter<T> converter, string initialMessage, string onFailureMessage, TimeSpan? timeoutOverride = null)
        {
            await ctx.RespondAsync(initialMessage).ConfigureAwait(false);

            while (true)
            {
                InteractivityResult<DiscordMessage> result = await ctx.Message.GetNextMessageAsync(timeoutOverride).ConfigureAwait(false);

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

        private async Task<Optional<DateTimeOffset>> GetTime(CommandContext ctx)
        {
            Optional<DateTimeOffset> sendTime = Optional.FromNoValue<DateTimeOffset>();
            while (true)
            {
                sendTime = await GetValueFromMessageWithReattemptsAsync(
                    ctx,
                    _dateTimeOffsetConverter,
                    $"What date and time **(in UTC)** would you like to send the message at? ({DATETIME_FORMAT}, e.g. {DATETIME_FORMAT_EXAMPLE})",
                    $"Could not parse that date, please use the format {DATETIME_FORMAT}, e.g. {DATETIME_FORMAT_EXAMPLE} for a **UTC** date/time").ConfigureAwait(false);

                if (!sendTime.HasValue)
                    return Optional.FromNoValue<DateTimeOffset>();
                else if (sendTime.Value < DateTimeOffset.UtcNow)
                    await ctx.RespondAsync("You can't pick a date/time in the past!").ConfigureAwait(false);
                else
                    return sendTime.Value;
            }
        }

        private async Task<Optional<Tuple<NotificationRepeatPeriod, uint>>> GetRepeat(CommandContext ctx)
        {
            const string request = "How often would you like this notification to repeat?\n" +
                ":zero:: No Repeat\n" +
                ":one:: Daily\n" +
                ":two:: Weekly\n" +
                ":three:: Monthly";

            DiscordMessage message = await ctx.RespondAsync(request).ConfigureAwait(false);
            await message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":zero:")).ConfigureAwait(false);
            await message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":one:")).ConfigureAwait(false);
            await message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":two:")).ConfigureAwait(false);
            await message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":three:")).ConfigureAwait(false);

            InteractivityResult<MessageReactionAddEventArgs> reaction = await message.WaitForReactionAsync(ctx.User).ConfigureAwait(false);
            if (reaction.TimedOut)
                return Optional.FromNoValue<Tuple<NotificationRepeatPeriod, uint>>();

            NotificationRepeatPeriod repeatPeriod = NotificationRepeatPeriod.NoRepeat;
            DiscordEmoji emoji = reaction.Result.Emoji;
            switch (emoji.GetDiscordName())
            {
                case "zero":
                    repeatPeriod = NotificationRepeatPeriod.NoRepeat;
                    break;
                case "one":
                    repeatPeriod = NotificationRepeatPeriod.Daily;
                    break;
                case "two":
                    repeatPeriod = NotificationRepeatPeriod.Weekly;
                    break;
                case "three":
                    repeatPeriod = NotificationRepeatPeriod.Monthly;
                    break;
            }

            if (repeatPeriod == NotificationRepeatPeriod.NoRepeat)
                return new Tuple<NotificationRepeatPeriod, uint>(repeatPeriod, 0);

            Optional<uint> repeatCount = await GetValueFromMessageWithReattemptsAsync<uint>(
                ctx,
                _uintConverter,
                "Specify, if applicable, the maximum number of times this notification should repeat. Otherwise, enter `0`",
                $"Please enter a number between 0 and {uint.MaxValue},").ConfigureAwait(false);

            if (repeatCount.HasValue)
                return new Tuple<NotificationRepeatPeriod, uint>(repeatPeriod, repeatCount.Value);
            else
                return new Tuple<NotificationRepeatPeriod, uint>(repeatPeriod, 0);
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
