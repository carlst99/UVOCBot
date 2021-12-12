using System;

namespace UVOCBot.Discord.Core;

public static class ComponentIdFormatter
{
    public const char SEPARATOR = ':';

    public static string GetId(string key, string payload)
    {
        if (key.Contains(SEPARATOR))
            throw new ArgumentException($"The key must not contain the '{SEPARATOR}' character", nameof(key));

        return $"{key}{SEPARATOR}{payload}";
    }

    public static void Parse(string id, out string key, out string payload)
    {
        string[] parts = id.Split(SEPARATOR);
        key = parts[0];
        payload = parts[1];
    }
}
