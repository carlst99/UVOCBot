using System;

namespace UVOCBot.Plugins.Feeds;

/// <summary>
/// Enumerates the available feeds.
/// </summary>
[Flags]
public enum Feed : ulong
{
    None = 0,
    ForumAnnouncement = 1 << 0,
    ForumPatchNotes = 1 << 1,
    ForumPTSAnnouncement = 1 << 2,
    ForumPTSPatchNotes = 1 << 3,
    TwitterPlanetside = 1 << 4,
    TwitterWrel = 1 << 5,
    TwitterRPG = 1 << 6
}
