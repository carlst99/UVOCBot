﻿using System;
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
    TwitterPlanetside = 1 << 4,
    TwitterWrel = 1 << 5,
    TwitterRPG = 1 << 6,
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
        { Feed.TwitterPlanetside, "🐦 Official PlanetSide Twitter" },
        { Feed.TwitterWrel, "🐦 Wrel's Twitter" },
        { Feed.TwitterRPG, "🐦 Rogue Planet Game's Twitter" },
        { Feed.PatchNotifications, "🚨 Patch Notifications" },
        { Feed.News, "📜 News" }
    };
}
