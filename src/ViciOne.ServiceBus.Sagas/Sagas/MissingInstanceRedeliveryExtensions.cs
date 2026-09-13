using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Configures delayed retries when a correlated saga instance does not exist.</summary>
public static class MissingInstanceRedeliveryExtensions
{
    /// <summary>
    /// Builds a missing-instance pipe that reschedules the consumed message according to a retry
    /// policy and increments its redelivery count. Scheduler-based redelivery is enabled by
    /// default and requires a configured message scheduler; callers can select transport delay
    /// through <see cref="IMissingInstanceRedeliveryConfigurator.ConfigureMessageScheduler" />.
    /// </summary>
    /// <typeparam name="TInstance">The saga instance type.</typeparam>
    /// <typeparam name="TData">The correlated message type.</typeparam>
    /// <param name="configurator">The missing-instance branch to replace.</param>
    /// <param name="configure">The retry, terminal, observer, and scheduling configuration.</param>
    /// <returns>The configured missing-instance redelivery pipe.</returns>
    public static IPipe<ConsumeContext<TData>> Redeliver<TInstance, TData>(this IMissingInstanceConfigurator<TInstance, TData> configurator,
        Action<IMissingInstanceRedeliveryConfigurator<TInstance, TData>> configure)
        where TInstance : SagaStateMachineInstance
        where TData : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(configure);

        var specification = new MissingInstanceRedeliveryConfigurator<TInstance, TData>(configurator);

        configure(specification);

        IReadOnlyList<ValidationResult> result = specification.Validate().ThrowIfContainsFailure();

        try
        {
            return specification.Build();
        }
        catch (Exception ex)
        {
            throw new ConfigurationException(result, global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Saga", "unknown", "The missing instance redelivery configuration was invalid", "Correct the named configuration before starting the host"), ex);
        }
    }
}
