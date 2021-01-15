using System;

namespace UVOCBot.Core.Model
{
    public enum NotificationRepeatPeriod
    {
        None,
        Daily,
        Weekly,
        Monthly
    }

    public sealed class ScheduledNotificationDTO
    {
        /// <summary>
        /// Gets the ID of this notification
        /// </summary>
        public ulong Id { get; set; }

        /// <summary>
        /// Gets the guild that has set this notification
        /// </summary>
        public ulong GuildId { get; set; }

        /// <summary>
        /// Gets or sets the time that the notification is set for
        /// </summary>
        public DateTimeOffset Time { get; set; }

        /// <summary>
        /// Gets or sets the last time that this notification was sent
        /// </summary>
        public DateTimeOffset LastNotifiedTime { get; set; }

        /// <summary>
        /// Gets or sets the period on which this notification should repeat
        /// </summary>
        public NotificationRepeatPeriod Repeat { get; set; }

        /// <summary>
        /// Gets or sets the total number of times this notification should repeat
        /// </summary>
        public int RepeatCount { get; set; }

        /// <summary>
        /// Gets or sets the id of the channel in which to send the notification
        /// </summary>
        public ulong ChannelId { get; set; }

        /// <summary>
        /// Gets or sets the id of the role to ping
        /// </summary>
        public ulong RoleId { get; set; }

        /// <summary>
        /// Gets or sets the content of the notification message
        /// </summary>
        public string MessageContent { get; set; }

        public override bool Equals(object obj) => obj is ScheduledNotificationDTO s
            && s.Id.Equals(Id);

        public override int GetHashCode() => Id.GetHashCode();
    }
}
