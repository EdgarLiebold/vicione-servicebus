using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides extension methods for saga pipe configurator.</summary>
public static class SagaPipeConfiguratorExtensions
{
    /// <summary>Adds a filter to the pipe.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="T">The context type.</typeparam>
    /// <param name="configurator">The pipe configurator.</param>
    /// <param name="filter">The already built pipe.</param>
    public static void UseFilter<TSaga, T>(this IPipeConfigurator<SagaConsumeContext<TSaga, T>> configurator,
        IFilter<SagaConsumeContext<TSaga>> filter)
        where T : class
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(filter);

        var pipeBuilderConfigurator = new SagaFilterSpecification<TSaga, T>(filter);

        configurator.AddPipeSpecification(pipeBuilderConfigurator);
    }
}
