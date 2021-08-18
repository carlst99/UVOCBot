using System;

namespace UVOCBot.Commands.Utilities
{
    /// <summary>
    /// Indicates that the initial interaction response should be emphemeral, when the decorated command is invoked via an interaction.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class EphemeralAttribute : Attribute
    {
        public bool IsEphemeral { get; }

        public EphemeralAttribute(bool isEphemeral = true)
        {
            IsEphemeral = isEphemeral;
        }
    }
}
