using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides extension methods for dead letter.
/// </summary>
public static class DeadLetterExtensions
{
    /// <summary>
    /// Rescue exceptions via the alternate pipe
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="rescuePipe"></param>
    public static void UseDeadLetter(this IPipeConfigurator<ReceiveContext> configurator, IPipe<ReceiveContext> rescuePipe)
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));

        var rescueConfigurator = new DeadLetterPipeSpecification(rescuePipe);

        configurator.AddPipeSpecification(rescueConfigurator);
    }
}
