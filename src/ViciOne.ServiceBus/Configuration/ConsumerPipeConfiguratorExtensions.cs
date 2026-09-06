using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides extension methods for consumer pipe configurator.</summary>
public static class ConsumerPipeConfiguratorExtensions
{
    /// <summary>Adds a filter to the pipe.</summary>
    /// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
    /// <typeparam name="T">The context type.</typeparam>
    /// <param name="configurator">The pipe configurator.</param>
    /// <param name="filter">The already built pipe.</param>
    public static void UseFilter<TConsumer, T>(this IPipeConfigurator<ConsumerConsumeContext<TConsumer, T>> configurator,
        IFilter<ConsumerConsumeContext<TConsumer>> filter)
        where T : class
        where TConsumer : class
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));

        var specification = new ConsumerFilterSpecification<TConsumer, T>(filter);

        configurator.AddPipeSpecification(specification);
    }
}
