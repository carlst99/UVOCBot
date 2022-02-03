using System.ComponentModel;

namespace UVOCBot.Discord.Core;

/// <summary>
/// Enumerates the possible display formats for Timestamp Markdown.
/// </summary>
public enum TimestampStyle
{
    /// <summary>
    /// Short time: 16:20.
    /// </summary>
    [Description("Short time: 16:20")]
    ShortTime,

    /// <summary>
    /// Long time: 16:20:30.
    /// </summary>
    [Description("Long time: 16:20:30")]
    LongTime,

    /// <summary>
    /// Short date: 20/04/2021.
    /// </summary>
    [Description("Short date: 20/04/2021")]
    ShortDate,

    /// <summary>
    /// Long date: 20 April 2021.
    /// </summary>
    [Description("Long date: 20 April 2021")]
    LongDate,

    /// <summary>
    /// Short datetime: 20 April 2021 16:20.
    /// </summary>]
    [Description("Short datetime: 20 April 2021 16:20")]
    ShortDateTime,

    /// <summary>
    /// Long datetime: Tuesday, 20 April 2021 16:20.
    /// </summary>
    [Description("Long datetime: Tuesday, 20 April 2021 16:20")]
    LongDateTime,

    /// <summary>
    /// Relative time: 2 months ago.
    /// </summary>
    [Description("Relative time: 2 months ago")]
    RelativeTime
}
