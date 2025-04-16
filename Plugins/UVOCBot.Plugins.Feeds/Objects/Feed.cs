using System;
using System.Collections.Generic;

namespace UVOCBot.Plugins.Feeds.Objects;

/// <summary>
/// Enumerates the available feeds.
/// </summary>
[Flags]
public enum Feed : ulong
{
    ForumAnnouncement = 1 << 0,
    ForumPatchNotes = 1 << 1,
    ForumPTSAnnouncement = 1 << 2,
    PatchNotifications = 1 << 7,
    News = 1 << 8
}

public static class FeedDescriptions
{
    public static IReadOnlyDictionary<Feed, string> Get { get; } = new Dictionary<Feed, string>
    {
        { Feed.ForumAnnouncement, "📢 Forum Announcements" },
        { Feed.ForumPatchNotes, "🩹 Forum Patch Notes" },
        { Feed.ForumPTSAnnouncement, "📢 Forum PTS Announcements" },
        { Feed.PatchNotifications, "🚨 Patch Notifications" },
        { Feed.News, "📜 News" }
    };
}
