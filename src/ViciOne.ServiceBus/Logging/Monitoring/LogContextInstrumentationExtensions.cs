using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Logging.Internal;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Monitoring;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Logging.Monitoring;

/// <summary>Creates failure-isolated OpenTelemetry metric scopes for service-bus operations.</summary>
internal static class LogContextInstrumentationExtensions
{
    private static readonly ConditionalWeakTable<ILogContext, LogContextInstrumentationState> LogContextStates = new();
    private static readonly ConditionalWeakTable<IMeterFactory, Lazy<LogContextInstrumentationState>> MeterFactoryStates = new();
    private static readonly FrozenDictionary<string, string> MessagingSystemAliases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["loopback"] = ServiceBusTelemetry.MessagingSystems.InMemory,
            ["in-memory"] = ServiceBusTelemetry.MessagingSystems.InMemory,
            ["rabbitmq"] = ServiceBusTelemetry.MessagingSystems.RabbitMq,
            ["activemq"] = ServiceBusTelemetry.MessagingSystems.ActiveMq,
            ["aws.sns"] = ServiceBusTelemetry.MessagingSystems.AmazonSns,
            ["amazonsqs"] = ServiceBusTelemetry.MessagingSystems.AmazonSqs,
            ["amazon_sqs"] = ServiceBusTelemetry.MessagingSystems.AmazonSqs,
            ["aws-sqs"] = ServiceBusTelemetry.MessagingSystems.AmazonSqs,
            ["aws_sqs"] = ServiceBusTelemetry.MessagingSystems.AmazonSqs,
            ["eventhubs"] = ServiceBusTelemetry.MessagingSystems.AzureEventHubs,
            ["sb"] = ServiceBusTelemetry.MessagingSystems.AzureServiceBus,
            ["azure-service-bus"] = ServiceBusTelemetry.MessagingSystems.AzureServiceBus,
            ["servicebus"] = ServiceBusTelemetry.MessagingSystems.AzureServiceBus,
            ["db"] = ServiceBusTelemetry.MessagingSystems.Sql,
            ["sql"] = ServiceBusTelemetry.MessagingSystems.Sql,
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    private static readonly object BindingLock = new();
    private static readonly object FallbackLock = new();

    private static LogContextInstrumentationState? _fallbackState;

    /// <summary>Starts metrics for one transport receive operation.</summary>
    /// <param name="logContext">The log context bound to the active meter.</param>
    /// <param name="context">The receive context that supplies transport identity and time.</param>
    /// <returns>A completion scope, or <see langword="null"/> when instrumentation is unavailable or disabled.</returns>
    public static MetricOperation? StartReceiveInstrument(this ILogContext logContext, ReceiveContext context) =>
        TryStart(logContext, context, state =>
        {
            if (!state.ConsumedMessages.Enabled
                && !state.ClientOperationDuration.Enabled
                && !state.ActiveOperations.Enabled)
                return null;

            var timeProvider = context.GetTimeProvider();
            long started = timeProvider.GetTimestamp();
            TagList tags = ClientTags(SystemName(context), "receive", "receive");

            Observe(() => state.ActiveOperations.Add(1, tags));

            return new MetricOperation(exception =>
            {
                Observe(() => state.ActiveOperations.Add(-1, tags));
                TagList completed = WithError(tags, exception);
                Observe(() => state.ConsumedMessages.Add(1, tags));
                Observe(() => state.ClientOperationDuration.Record(ElapsedSeconds(timeProvider, started), completed));
            });
        });

    /// <summary>Starts processing metrics for a message handled by a delegate.</summary>
    /// <typeparam name="TMessage">The handled message contract.</typeparam>
    /// <param name="logContext">The log context bound to the active meter.</param>
    /// <param name="context">The active consume context.</param>
    /// <returns>A completion scope, or <see langword="null"/> when instrumentation is unavailable or disabled.</returns>
    public static MetricOperation? StartHandlerInstrument<TMessage>(
        this ILogContext logContext,
        ConsumeContext<TMessage> context)
        where TMessage : class =>
        StartProcess(logContext, context, "handle", "handler");

    /// <summary>Starts processing metrics for a message delivered to a consumer.</summary>
    /// <typeparam name="TConsumer">The consumer implementation.</typeparam>
    /// <typeparam name="T">The consumed message contract.</typeparam>
    /// <param name="logContext">The log context bound to the active meter.</param>
    /// <param name="context">The active consume context.</param>
    /// <returns>A completion scope, or <see langword="null"/> when instrumentation is unavailable or disabled.</returns>
    public static MetricOperation? StartConsumeInstrument<TConsumer, T>(
        this ILogContext logContext,
        ConsumeContext<T> context)
        where T : class =>
        StartProcess(
            logContext,
            context,
            ConsumerProcessorIdentity<TConsumer>.OperationName,
            ConsumerProcessorIdentity<TConsumer>.ProcessorKind);

    /// <summary>Starts metrics for one transport send operation.</summary>
    /// <typeparam name="T">The sent message contract.</typeparam>
    /// <param name="logContext">The calling log context; the transport-bound context owns the metric binding.</param>
    /// <param name="transportContext">The transport context that supplies the metric binding and transport identity.</param>
    /// <param name="context">The active send context.</param>
    /// <returns>A completion scope, or <see langword="null"/> when instrumentation is unavailable or disabled.</returns>
    public static MetricOperation? StartSendInstrument<T>(
        this ILogContext logContext,
        SendTransportContext transportContext,
        SendContext<T> context)
        where T : class =>
        TryStart(transportContext.LogContext, context, state =>
        {
            if (!state.SentMessages.Enabled
                && !state.ClientOperationDuration.Enabled
                && !state.ActiveOperations.Enabled)
                return null;

            var timeProvider = context.GetTimeProvider();
            long started = timeProvider.GetTimestamp();
            TagList tags = ClientTags(NormalizeSystem(transportContext.ActivitySystem), "send", "send");

            Observe(() => state.ActiveOperations.Add(1, tags));

            return new MetricOperation(exception =>
            {
                Observe(() => state.ActiveOperations.Add(-1, tags));
                TagList completed = WithError(tags, exception);
                Observe(() => state.SentMessages.Add(1, completed));
                Observe(() => state.ClientOperationDuration.Record(ElapsedSeconds(timeProvider, started), completed));
            });
        });

    /// <summary>Starts an outcome counter scope for one outbox enqueue operation.</summary>
    /// <param name="logContext">The log context bound to the active meter.</param>
    /// <returns>A completion scope, or <see langword="null"/> when instrumentation is unavailable or disabled.</returns>
    public static MetricOperation? StartOutboxEnqueueInstrument(this ILogContext logContext) =>
        StartOutbox(logContext, "enqueue");

    /// <summary>Starts an outcome counter scope for one outbox delivery operation.</summary>
    /// <param name="logContext">The log context bound to the active meter.</param>
    /// <returns>A completion scope, or <see langword="null"/> when instrumentation is unavailable or disabled.</returns>
    public static MetricOperation? StartOutboxDeliveryInstrument(this ILogContext logContext) =>
        StartOutbox(logContext, "deliver");

    internal static void TryConfigure(IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        LogContext.Current = new BusLogContext(NullLoggerFactory.Instance);
        try
        {
            ILoggerFactory loggerFactory = provider.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
            LogContext.Current = new BusLogContext(loggerFactory);

            IMeterFactory? meterFactory = provider.GetService<IMeterFactory>();
            if (meterFactory == null)
                return;

            Lazy<LogContextInstrumentationState> lazyState = MeterFactoryStates.GetValue(meterFactory, key =>
                new Lazy<LogContextInstrumentationState>(() =>
                {
                    Meter meter = key.Create(new MeterOptions(ServiceBusTelemetry.MeterName)
                    {
                        Version = HostMetadataCache.Host.ViciOneServiceBusVersion,
                    });
                    return new LogContextInstrumentationState(meter, new BusLogContext(loggerFactory));
                }, LazyThreadSafetyMode.ExecutionAndPublication));

            LogContextInstrumentationState instrumentation = lazyState.Value;
            BindInstrumentation(instrumentation.RootLogContext, instrumentation);
            LogContext.Current = instrumentation.RootLogContext;
        }
        catch
        {
            // Instrument creation belongs to the application observation boundary. A broken custom
            // meter factory or listener disables metrics for this activation, not the service bus.
            // The provider-specific uninstrumented log context installed above ensures that each
            // activation owns its meter scope.
        }
    }

    internal static void TryConfigure()
    {
        try
        {
            if (Volatile.Read(ref _fallbackState) == null)
            {
                lock (FallbackLock)
                {
                    if (_fallbackState == null)
                    {
                        var meter = new Meter(ServiceBusTelemetry.MeterName, HostMetadataCache.Host.ViciOneServiceBusVersion);
                        ILogContext root = LogContext.Current ?? new BusLogContext(NullLoggerFactory.Instance);
                        Volatile.Write(ref _fallbackState, new LogContextInstrumentationState(meter, root));
                    }
                }
            }

            LogContextInstrumentationState? fallback = Volatile.Read(ref _fallbackState);
            if (fallback == null)
                return;

            ILogContext current = LogContext.Current ?? fallback.RootLogContext;
            LogContext.Current = current;
            BindInstrumentation(current, fallback);
        }
        catch
        {
            // The explicit non-DI path follows the same no-impact rule as the DI path.
        }
    }

    internal static void CopyInstrumentation(ILogContext? source, ILogContext? destination)
    {
        if (source != null && destination != null && LogContextStates.TryGetValue(source, out LogContextInstrumentationState? instrumentation))
            BindInstrumentation(destination, instrumentation);
    }

    internal static MetricOperation? StartProcess(ILogContext logContext, PipeContext context, string operationName, string processorKind) =>
        TryStart(logContext, context, state =>
        {
            if (!state.ProcessDuration.Enabled
                && !state.ActiveOperations.Enabled
                && !state.RetryAttempts.Enabled
                && !state.DeliveryDuration.Enabled)
                return null;

            var timeProvider = context.GetTimeProvider();
            long started = timeProvider.GetTimestamp();
            string system = context is ConsumeContext consume
                ? SystemName(consume.ReceiveContext)
                : "unknown";
            TagList tags = ProcessTags(system, operationName, processorKind);

            Observe(() => state.ActiveOperations.Add(1, tags));

            if (context is ConsumeContext consumeContext)
            {
                if (consumeContext.GetRetryAttempt() > 0)
                    Observe(() => state.RetryAttempts.Add(1, tags));

                if (consumeContext.SentTime.HasValue)
                {
                    double deliverySeconds = Math.Max(0, (timeProvider.GetUtcNow() - AsUtc(consumeContext.SentTime.Value)).TotalSeconds);
                    Observe(() => state.DeliveryDuration.Record(deliverySeconds, tags));
                }
            }

            return new MetricOperation(exception =>
            {
                Observe(() => state.ActiveOperations.Add(-1, tags));
                TagList completed = WithError(tags, exception);
                Observe(() => state.ProcessDuration.Record(ElapsedSeconds(timeProvider, started), completed));
            });
        });

    private static MetricOperation? StartOutbox(ILogContext logContext, string operation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);

        return TryStart(logContext, state =>
        {
            if (!state.OutboxMessages.Enabled)
                return null;

            var tags = new TagList
            {
                { ServiceBusTelemetry.Attributes.OutboxOperation, operation },
            };

            return new MetricOperation(exception =>
            {
                TagList completed = tags;
                completed.Add(ServiceBusTelemetry.Attributes.Outcome, exception == null ? "succeeded" : "faulted");
                if (exception != null)
                    completed.Add(ServiceBusTelemetry.Attributes.ErrorType, ErrorType(exception));
                Observe(() => state.OutboxMessages.Add(1, completed));
            });
        });
    }

    private static MetricOperation? TryStart(
        ILogContext? logContext,
        object context,
        Func<LogContextInstrumentationState, MetricOperation?> start)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            LogContextInstrumentationState? instrumentation = GetInstrumentation(logContext);
            return instrumentation == null ? null : start(instrumentation);
        }
        catch
        {
            return null;
        }
    }

    private static MetricOperation? TryStart(
        ILogContext? logContext,
        Func<LogContextInstrumentationState, MetricOperation?> start)
    {
        try
        {
            LogContextInstrumentationState? instrumentation = GetInstrumentation(logContext);
            return instrumentation == null ? null : start(instrumentation);
        }
        catch
        {
            return null;
        }
    }

    private static void BindInstrumentation(ILogContext logContext, LogContextInstrumentationState instrumentation)
    {
        lock (BindingLock)
        {
            LogContextStates.Remove(logContext);
            LogContextStates.Add(logContext, instrumentation);
        }
    }

    private static LogContextInstrumentationState? GetInstrumentation(ILogContext? logContext) =>
        logContext != null && LogContextStates.TryGetValue(logContext, out LogContextInstrumentationState? instrumentation)
            ? instrumentation
            : null;

    private static TagList ClientTags(string system, string operationName, string operationType) =>
        new()
        {
            { ServiceBusTelemetry.Attributes.MessagingSystem, system },
            { ServiceBusTelemetry.Attributes.OperationName, operationName },
            { ServiceBusTelemetry.Attributes.OperationType, operationType },
        };

    private static TagList ProcessTags(string system, string operationName, string processorKind) =>
        new()
        {
            { ServiceBusTelemetry.Attributes.MessagingSystem, system },
            { ServiceBusTelemetry.Attributes.OperationName, operationName },
            { ServiceBusTelemetry.Attributes.OperationType, "process" },
            { ServiceBusTelemetry.Attributes.ProcessorKind, processorKind },
        };

    private static TagList WithError(TagList tags, Exception? exception)
    {
        if (exception != null)
            tags.Add(ServiceBusTelemetry.Attributes.ErrorType, ErrorType(exception));

        return tags;
    }

    private static string ErrorType(Exception exception)
    {
        Type type = (exception.GetBaseException() ?? exception).GetType();
        return type.FullName ?? type.Name;
    }

    private static double ElapsedSeconds(TimeProvider timeProvider, long started) =>
        Math.Max(0, timeProvider.GetElapsedTime(started).TotalSeconds);

    private static DateTimeOffset AsUtc(DateTimeOffset value) => value.ToUniversalTime();

    private static string SystemName(Uri? address) =>
        NormalizeSystem(address?.Scheme);

    internal static string SystemName(ReceiveContext context) =>
        context.TryGetPayload<TransportReceiveContext>(out TransportReceiveContext? transport)
        && transport.ActivitySystem.Length > 0
            ? NormalizeSystem(transport.ActivitySystem)
            : SystemName(context.InputAddress);

    internal static string NormalizeSystem(string? system)
    {
        if (string.IsNullOrEmpty(system))
            return ServiceBusTelemetry.MessagingSystems.Unknown;

        return MessagingSystemAliases.TryGetValue(system, out string? normalized)
            ? normalized
            : ServiceBusTelemetry.MessagingSystems.Other;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Observe(Action observation)
    {
        try
        {
            observation();
        }
        catch
        {
            // Application-owned listeners are observational only.
        }
    }

    private static class ConsumerProcessorIdentity<TConsumer>
    {
        private static readonly bool IsHandlerAdapter = typeof(TConsumer).IsDefined(typeof(HandlerConsumerAdapterAttribute), inherit: false);

        public static string OperationName => IsHandlerAdapter ? "handle" : "consume";
        public static string ProcessorKind => IsHandlerAdapter ? "handler" : "consumer";
    }
}
