namespace Remora.Discord.API.Abstractions.Objects;

public static class IMessageExtensions
{
    public static string GetUrl(this IMessage message)
    {
        string guildId = "@me";
        if (message.GuildID.HasValue)
            guildId = message.GuildID.Value.Value.ToString();

        return $"https://discord.com/channels/{ guildId }/{ message.ChannelID.Value }/{ message.ID.Value }";
    }
}
