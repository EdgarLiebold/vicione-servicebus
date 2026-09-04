using System;

namespace ViciOne.ServiceBus.Contracts;

/// <summary>
/// Defines the contract for command context.
/// </summary>
public interface CommandContext :
    PipeContext
{
    /// <summary>
    /// The timestamp at which the command was sent
    /// </summary>
    DateTimeOffset Timestamp { get; }
}


/// <summary>
/// Defines the contract for command context.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public interface CommandContext<out T> :
    CommandContext
    where T : class
{
    /// <summary>
    /// The command object
    /// </summary>
    T Command { get; }
}
