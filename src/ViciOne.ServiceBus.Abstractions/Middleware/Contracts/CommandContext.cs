using System;

namespace ViciOne.ServiceBus.Contracts;

public interface CommandContext :
    PipeContext
{
    /// <summary>
    /// The timestamp at which the command was sent
    /// </summary>
    DateTimeOffset Timestamp { get; }
}


public interface CommandContext<out T> :
    CommandContext
    where T : class
{
    /// <summary>
    /// The command object
    /// </summary>
    T Command { get; }
}
