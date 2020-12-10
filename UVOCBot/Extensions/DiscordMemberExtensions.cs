using DSharpPlus.Entities;

namespace UVOCBot.Extensions
{
    public static class DiscordMemberExtensions
    {
        /// <summary>
        /// Returns either a user's nickname, or their username stripped of the unique tag
        /// </summary>
        /// <param name="member"></param>
        public static string GetFriendlyName(this DiscordMember member)
        {
            if (!string.IsNullOrEmpty(member.Nickname))
                return member.Nickname;
            else
                return member.Username.Split("#")[0];
        }
    }
}
