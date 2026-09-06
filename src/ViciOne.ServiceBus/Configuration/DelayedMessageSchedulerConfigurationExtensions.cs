using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides extension methods for delayed message scheduler configuration.</summary>
public static class DelayedMessageSchedulerConfigurationExtensions
{
    /// <summary>Use the built-in transport message delay to schedule messages.</summary>
    /// <param name="configurator">The configurator to update.</param>
    public static void ConfigureDelayedMessageScheduler(this IBusFactoryConfigurator configurator)
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));

        var specification = new DelayedMessageSchedulerSpecification();

        configurator.AddPrePipeSpecification(specification);
    }
}
