using System;
using System.Collections.Generic;

namespace UVOCBot.Discord.Core.Components;

/// <summary>
/// Represents a repository of <see cref="IComponentResponder"/> types.
/// </summary>
internal class ComponentResponderRepository
{
    private static readonly IReadOnlyList<Type> SharedEmptyList = new List<Type>();

    private readonly Dictionary<string, List<Type>> _responders;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComponentResponderRepository"/>.
    /// </summary>
    public ComponentResponderRepository()
    {
        _responders = new Dictionary<string, List<Type>>();
    }

    /// <summary>
    /// Gets all the <see cref="IComponentResponder"/> types that were registered for the given key.
    /// </summary>
    /// <param name="key">The key of the responder.</param>
    /// <returns>A list of <see cref="IComponentResponder"/> types.</returns>
    public IReadOnlyList<Type> GetResponders(string key)
    {
        if (_responders.TryGetValue(key, out List<Type>? result))
            return result;
        else
            return SharedEmptyList;
    }

    /// <summary>
    /// Adds a <see cref="IComponentResponder"/> type to the repository.
    /// </summary>
    /// <typeparam name="TResponder">The type of the responder.</typeparam>
    /// <param name="key">The component key that the responder is registered to.</param>
    public void AddResponder<TResponder>(string key) where TResponder : class, IComponentResponder
    {
        if (!_responders.ContainsKey(key))
            _responders[key] = new List<Type>();

        _responders[key].Add(typeof(TResponder));
    }
}
