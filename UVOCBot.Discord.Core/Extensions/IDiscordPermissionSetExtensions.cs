namespace Remora.Discord.API.Abstractions.Objects
{
    public static class IDiscordPermissionSetExtensions
    {
        /// <summary>
        /// Checks if the permission set contains the administrator permission, otherwise checks that the set contains the given permission.
        /// </summary>
        /// <param name="set">The set to check.</param>
        /// <param name="permission">The permission to check for, if that administrator permission is not part of the set.</param>
        /// <returns>A value indicating if the set indicates the given permission is granted.</returns>
        public static bool HasAdminOrPermission(this IDiscordPermissionSet set, DiscordPermission permission)
        {
            if (set.HasPermission(DiscordPermission.Administrator))
                return true;

            return set.HasPermission(permission);
        }
    }
}
