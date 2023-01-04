using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace Remora.Discord.API.Abstractions.Objects;

/// <summary>
/// Contains extension methods for the <see cref="IInteraction"/> class.
/// </summary>
public static class IInteractionExtensions
{
    /// <summary>
    /// Attempts to retrieve a <see cref="IUser"/> from an interaction.
    /// </summary>
    /// <param name="interaction">The interaction.</param>
    /// <param name="user">The user, or <c>null</c> if none was present.</param>
    /// <returns><c>True</c> if a user was present on the interaction.</returns>
    public static bool TryGetUser(this IInteraction interaction, [NotNullWhen(true)] out IUser? user)
    {
        user = null;

        if (interaction.User.IsDefined(out user))
            return true;

        return interaction.Member.HasValue && interaction.Member.Value.User.IsDefined(out user);
    }
}
