using System;

namespace ViciOne.ServiceBus.Contracts;

/// <summary>Exposes state for command operations.</summary>
public interface CommandContext :
    PipeContext
{
    /// <summary>The timestamp at which the command was sent.</summary>
    DateTimeOffset Timestamp { get; }
}


/// <summary>Exposes state for command operations.</summary>
/// <typeparam name="T">The value type.</typeparam>
public interface CommandContext<out T> :
    CommandContext
    where T : class
{
    /// <summary>The command object.</summary>
    T Command { get; }
}
