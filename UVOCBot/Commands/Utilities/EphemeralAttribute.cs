using System;

namespace UVOCBot.Commands.Utilities
{
    /// <summary>
    /// Indicates that the initial interaction response should be emphemeral, when the decorated command is invoked via an interaction.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class EphemeralAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets a value indicating whether this command should send ephemeral responses.
        /// </summary>
        public bool IsEphemeral { get; }

        /// <summary>
        /// Initialises a new instance of the <see cref="EphemeralAttribute"/> attribute.
        /// </summary>
        /// <param name="isEphemeral">A value indicating whether this command should send ephemeral responses. Set this to override group-level <see cref="EphemeralAttribute"/>s.</param>
        public EphemeralAttribute(bool isEphemeral = true)
        {
            IsEphemeral = isEphemeral;
        }
    }
}
