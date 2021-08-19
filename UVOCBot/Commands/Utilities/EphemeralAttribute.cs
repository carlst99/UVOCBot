using System;

namespace UVOCBot.Commands.Utilities
{
    /// <summary>
    /// Marks a command as requiring an ephemeral response, when invoked by an interaction.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class EphemeralAttribute : Attribute
    {
        /// <summary>
        /// Gets a value indicating whether this command should send ephemeral responses.
        /// </summary>
        public bool IsEphemeral { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EphemeralAttribute"/> class.
        /// </summary>
        /// <param name="isEphemeral">A value indicating whether this command should send ephemeral responses. Set this to override group-level <see cref="EphemeralAttribute"/>s.</param>
        public EphemeralAttribute(bool isEphemeral = true)
        {
            IsEphemeral = isEphemeral;
        }
    }
}
