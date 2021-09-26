using System.ComponentModel;

namespace UVOCBot.Discord.Core
{
    /// <summary>
    /// Represents the possible display formats for Timestamp Markdown.
    /// </summary>
    public enum TimestampStyle
    {
        /// <summary>
        /// 16:20.
        /// </summary>
        [Description("Formats the timestamp as a short time, e.g. 16:20.")]
        ShortTime,

        /// <summary>
        /// 16:20:30.
        /// </summary>
        [Description("Formats the timestamp as a long time, e.g. 16:20:30.")]
        LongTime,

        /// <summary>
        /// 20/04/2021.
        /// </summary>
        [Description("Formats the timestamp as a short date, e.g. 20/04/2021.")]
        ShortDate,

        /// <summary>
        /// 20 April 2021.
        /// </summary>
        [Description("Formats the timestamp as a long date, e.g. 20 April 2021.")]
        LongDate,

        /// <summary>
        /// 20 April 2021 16:20.
        /// </summary>]
        [Description("Formats the timestamp as a short datetime, e.g. 20 April 2021 16:20.")]
        ShortDateTime,

        /// <summary>
        /// Tuesday, 20 April 2021 16:20.
        /// </summary>
        [Description("Formats the timestamp as a long datetime, e.g. Tuesday, 20 April 2021 16:20.")]
        LongDateTime,

        /// <summary>
        /// 2 months ago.
        /// </summary>
        [Description("Formats the timestamp as a relative time, e.g. 2 months ago.")]
        RelativeTime
    }
}
