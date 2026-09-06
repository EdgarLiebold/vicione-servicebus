using System;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides extension methods for correlation id convention.</summary>
public static class CorrelationIdConventionExtensions
{
    /// <summary>
    /// Specify for the message type that the delegate be used for setting the CorrelationId
    /// property of the message envelope.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="correlationIdSelector">The correlation id selector.</param>
    public static void UseCorrelationId<T>(this IMessageSendTopologyConfigurator<T> configurator, Func<T, Guid> correlationIdSelector)
        where T : class
    {
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

    /// <summary>
    /// Specify for the message type that the delegate be used for setting the CorrelationId
    /// property of the message envelope.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="correlationIdSelector">The correlation id selector.</param>
    public static void UseCorrelationId<T>(this IMessageSendTopologyConfigurator<T> configurator, Func<T, Guid?> correlationIdSelector)
        where T : class
    {
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

    /// <summary>
    /// Specify for the message type that the delegate be used for setting the CorrelationId
    /// property of the message envelope.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="correlationIdSelector">The correlation id selector.</param>
    public static void UseCorrelationId<T>(this ISendTopology configurator, Func<T, Guid> correlationIdSelector)
        where T : class
    {
        configurator.GetMessageTopology<T>().UseCorrelationId(correlationIdSelector);
    }

    /// <summary>
    /// Specify for the message type that the delegate be used for setting the CorrelationId
    /// property of the message envelope.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="correlationIdSelector">The correlation id selector.</param>
    public static void UseCorrelationId<T>(this ISendTopology configurator, Func<T, Guid?> correlationIdSelector)
        where T : class
    {
        configurator.GetMessageTopology<T>().UseCorrelationId(correlationIdSelector);
    }
}
