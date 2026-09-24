using System;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Configures consumer batches that preserve Azure Service Bus session ordering.</summary>
public static class ServiceBusBatchingExtensions
{
    /// <summary>
    /// Configures <see cref="BatchOptions" /> to group messages by Azure Service Bus session identifier.
    /// Each batch contains at most <see cref="ServiceBusSessionBatchOptions.MessageLimitPerSession" /> messages from one session,
    /// while up to <see cref="ServiceBusSessionBatchOptions.MaxConcurrentSessions" /> sessions can be processed concurrently.
    /// The endpoint processor is configured for sessions and allows enough concurrent callbacks to fill each batch.
    /// </summary>
    /// <typeparam name="TConsumer">The consumer receiving the session batches.</typeparam>
    /// <param name="consumerConfigurator">The consumer registration to configure.</param>
    /// <param name="configure">Configures the session batch limits and timing.</param>
    public static void SetServiceBusSessionBatchOptions<TConsumer>(this IConsumerConfigurator<TConsumer> consumerConfigurator,
        Action<ServiceBusSessionBatchOptions> configure)
        where TConsumer : class
    {
        ArgumentNullException.ThrowIfNull(consumerConfigurator);
        ArgumentNullException.ThrowIfNull(configure);
        ServiceBusSessionBatchOptions sessionOptions = new();
        configure(sessionOptions);
        sessionOptions.Validate();

        consumerConfigurator.Options<BatchOptions>(o =>
        {
            o.GroupBy<object, string>(e => e.Advanced().SessionId())
                .SetConcurrencyLimit(sessionOptions.MaxConcurrentSessions)
                .SetMessageLimit(sessionOptions.MessageLimitPerSession)
                .SetTimeLimit(sessionOptions.TimeLimit)
                .SetTimeLimitStart(sessionOptions.TimeLimitStart)
                .SetConfigurationCallback((name, configurator) =>
                {
                    if (configurator is not IServiceBusEndpointConfigurator sb)
                        throw new ArgumentException($"Expecting {nameof(IServiceBusEndpointConfigurator)}", nameof(configurator));

                    sb.RequiresSession = true;

                    sb.MaxConcurrentSessions = sessionOptions.MaxConcurrentSessions;
                    sb.MaxConcurrentCallsPerSession = sessionOptions.MessageLimitPerSession;
                    sb.SessionIdleTimeout = sessionOptions.SessionIdleTimeout;

                    if (configurator.PrefetchCount != 0 && configurator.PrefetchCount < sessionOptions.MessageLimitPerSession)
                        configurator.PrefetchCount = sessionOptions.MessageLimitPerSession;
                });
        });
    }
}
