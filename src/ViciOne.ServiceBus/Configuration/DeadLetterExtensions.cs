using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides extension methods for dead letter.</summary>
public static class DeadLetterExtensions
{
    /// <summary>Adds a dead-letter pipeline for messages that remain undelivered and unfaulted after the receive pipeline completes.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="rescuePipe">The pipeline for messages that remain undelivered and unfaulted.</param>
    public static void UseDeadLetter(this IPipeConfigurator<ReceiveContext> configurator, IPipe<ReceiveContext> rescuePipe)
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));

        var rescueConfigurator = new DeadLetterPipeSpecification(rescuePipe);

        configurator.AddPipeSpecification(rescueConfigurator);
    }
}
