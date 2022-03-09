using System;

namespace UVOCBot.Discord.Core.Commands.Attributes;

/// <summary>
/// Indicates that the command/group that this attribute
/// is applied to should use a deferred interaction response.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class DeferredAttribute : Attribute
{
}
