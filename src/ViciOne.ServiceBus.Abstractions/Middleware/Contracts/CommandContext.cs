using System;

namespace ViciOne.ServiceBus.Contracts;

/// <summary>Describes a timestamped control command traveling through a command pipeline.</summary>
public interface CommandContext :
    PipeContext
{
    /// <summary>Gets the UTC time at which the command context was created.</summary>
    DateTimeOffset Timestamp { get; }
}


/// <summary>Provides the typed payload of a control command.</summary>
/// <typeparam name="TCommand">The command contract type.</typeparam>
public interface CommandContext<out TCommand> :
    CommandContext
    where TCommand : class
{
    /// <summary>Gets the command payload.</summary>
    TCommand Command { get; }
}
