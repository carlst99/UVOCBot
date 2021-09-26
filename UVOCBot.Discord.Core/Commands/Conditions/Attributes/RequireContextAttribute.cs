using Remora.Commands.Conditions;

namespace UVOCBot.Discord.Core.Commands.Conditions.Attributes
{
    /// <summary>
    /// Enumerates various channel contexts.
    /// </summary>
    public enum ChannelContext
    {
        /// <summary>
        /// The command was executed in a guild.
        /// </summary>
        Guild,

        /// <summary>
        /// The command was executed in a DM.
        /// </summary>
        DM,

        /// <summary>
        /// The command was executed in a group DM.
        /// </summary>
        GroupDM
    }

    /// <summary>
    /// Marks a command as requiring execution within a particular context.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RequireContextAttribute : ConditionAttribute
    {
        /// <summary>
        /// Gets the command context.
        /// </summary>
        public ChannelContext Context { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequireContextAttribute"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        public RequireContextAttribute(ChannelContext context)
        {
            Context = context;
        }
    }
}
