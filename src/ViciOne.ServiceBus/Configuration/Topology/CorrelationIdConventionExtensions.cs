using System;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures correlation-identifier selection for message send topology.</summary>
public static class CorrelationIdConventionExtensions
{
    /// <summary>Uses a required correlation identifier selected from the message contract.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="configurator">The message send topology to configure.</param>
    /// <param name="correlationIdSelector">The selector that returns the correlation identifier.</param>
    public static void UseCorrelationId<T>(this IMessageSendTopologyConfigurator<T> configurator, Func<T, Guid> correlationIdSelector)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(correlationIdSelector);

        configurator.AddOrUpdateConvention<ICorrelationIdMessageSendTopologyConvention<T>>(
            () =>
            {
                var convention = new CorrelationIdMessageSendTopologyConvention<T>();
                convention.SetCorrelationId(new DelegateMessageCorrelationId<T>(correlationIdSelector));

                return convention;
            },
            update =>
            {
                update.SetCorrelationId(new DelegateMessageCorrelationId<T>(correlationIdSelector));

                return update;
            });
    }

    /// <summary>Uses an optional correlation identifier selected from the message contract.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="configurator">The message send topology to configure.</param>
    /// <param name="correlationIdSelector">The selector that returns the optional correlation identifier.</param>
    public static void UseCorrelationId<T>(this IMessageSendTopologyConfigurator<T> configurator, Func<T, Guid?> correlationIdSelector)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(correlationIdSelector);

        configurator.AddOrUpdateConvention<ICorrelationIdMessageSendTopologyConvention<T>>(
            () =>
            {
                var convention = new CorrelationIdMessageSendTopologyConvention<T>();
                convention.SetCorrelationId(new NullableDelegateMessageCorrelationId<T>(correlationIdSelector));

                return convention;
            },
            update =>
            {
                update.SetCorrelationId(new NullableDelegateMessageCorrelationId<T>(correlationIdSelector));

                return update;
            });
    }

    /// <summary>Uses a required correlation identifier for one message contract in a send topology.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="configurator">The send topology to configure.</param>
    /// <param name="correlationIdSelector">The selector that returns the correlation identifier.</param>
    public static void UseCorrelationId<T>(this ISendTopology configurator, Func<T, Guid> correlationIdSelector)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(correlationIdSelector);

        configurator.GetMessageTopology<T>().UseCorrelationId(correlationIdSelector);
    }

    /// <summary>Uses an optional correlation identifier for one message contract in a send topology.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="configurator">The send topology to configure.</param>
    /// <param name="correlationIdSelector">The selector that returns the optional correlation identifier.</param>
    public static void UseCorrelationId<T>(this ISendTopology configurator, Func<T, Guid?> correlationIdSelector)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(correlationIdSelector);

        configurator.GetMessageTopology<T>().UseCorrelationId(correlationIdSelector);
    }
}
