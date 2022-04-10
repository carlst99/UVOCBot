using System;

namespace UVOCBot.Plugins.Feeds.Objects;

public enum Manifest
{
    Live,
    LiveNext,
    PTS,
    PTSNext
}

public static class ManifestExtensions
{
    public static string GetUrl(this Manifest manifest)
        => manifest switch
        {
            Manifest.Live => "http://manifest.patch.daybreakgames.com/patch/sha/manifest/planetside2/planetside2-live/live/planetside2-live.sha.soe.txt",
            Manifest.LiveNext => "http://manifest.patch.daybreakgames.com/patch/sha/manifest/planetside2/planetside2-live/livenext/planetside2-live.sha.soe.txt",
            Manifest.PTS => "http://manifest.patch.daybreakgames.com/patch/sha/manifest/planetside2/planetside2-test/live/planetside2-test.sha.soe.txt",
            Manifest.PTSNext => "http://manifest.patch.daybreakgames.com/patch/sha/manifest/planetside2/planetside2-test/livenext/planetside2-test.sha.soe.txt",
            _ => throw new ArgumentException("The given manifest type is invalid", nameof(manifest))
        };
}
