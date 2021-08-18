using System;

namespace UVOCBot.Model
{
    /// <summary>
    /// Enumerates options for admin logging.
    /// </summary>
    [Flags]
    public enum AdminLogTypes : ulong
    {
        None = 0,

        /// <summary>
        /// Member join logs.
        /// </summary>
        MemberJoin = 1 << 0,

        /// <summary>
        /// Member leave logs.
        /// </summary>
        MemberLeave = 1 << 1
    }
}
