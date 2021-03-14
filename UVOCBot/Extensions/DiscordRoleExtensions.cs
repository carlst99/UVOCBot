using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DSharpPlus.Entities
{
    public static class DiscordRoleExtensions
    {
        /// <summary>
        /// Gets all members of a guild who have the specified role
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="role"></param>
        /// <returns></returns>
        public static async Task<List<DiscordMember>> GetMembers(this DiscordRole role, DiscordGuild guild)
        {
            IReadOnlyCollection<DiscordMember> guildMembers = await guild.GetAllMembersAsync().ConfigureAwait(false);
            List<DiscordMember> roleOwners = new List<DiscordMember>();

            foreach (DiscordMember member in guildMembers)
            {
                if (member.Roles.Contains(role))
                    roleOwners.Add(member);
            }

            return roleOwners;
        }
    }
}
