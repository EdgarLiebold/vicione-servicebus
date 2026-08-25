#nullable enable
namespace ViciOne.ServiceBus.Logging;

using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Monitoring;
using ViciOne.ServiceBus.Transports;


public static class LogContextInstrumentationExtensions
{
    private static readonly ConditionalWeakTable<ILogContext, LogContextInstrumentationState> LogContextStates = new();
    private static readonly ConditionalWeakTable<IMeterFactory, Lazy<LogContextInstrumentationState>> MeterFactoryStates = new();
    private static readonly object BindingLock = new();
    private static readonly object FallbackLock = new();

    private static LogContextInstrumentationState? _fallbackState;

    public static MetricOperation? StartReceiveInstrument(this ILogContext logContext, ReceiveContext context) =>
        TryStart(logContext, context, state =>
        {
            if (!state.ConsumedMessages.Enabled
                && !state.ClientOperationDuration.Enabled
                && !state.ActiveOperations.Enabled)
                return null;

            var timeProvider = context.GetTimeProvider();
            long started = timeProvider.GetTimestamp();
            TagList tags = ClientTags(SystemName(context.InputAddress), "receive", "receive");

            Observe(() => state.ActiveOperations.Add(1, tags));

            return new MetricOperation(exception =>
            {
                Observe(() => state.ActiveOperations.Add(-1, tags));
                TagList completed = WithError(tags, exception);
                Observe(() => state.ConsumedMessages.Add(1, tags));
                Observe(() => state.ClientOperationDuration.Record(ElapsedSeconds(timeProvider, started), completed));
            });
        });

    public static MetricOperation? StartHandlerInstrument<TMessage>(
        this ILogContext logContext,
        ConsumeContext<TMessage> context)
        where TMessage : class =>
        StartProcess(logContext, context, "handle", "handler");

    public static MetricOperation? StartSagaInstrument<TSaga, T>(
        this ILogContext logContext,
        SagaConsumeContext<TSaga, T> context)
        where T : class
        where TSaga : class, ISaga =>
        StartProcess(logContext, context, "saga", "saga");

    public static MetricOperation? StartSagaStateMachineInstrument<TSaga, T>(
        this ILogContext logContext,
        BehaviorContext<TSaga, T> context)
        where T : class
        where TSaga : class, SagaStateMachineInstance =>
        StartProcess(logContext, context, "saga", "saga_state_machine");

    public static MetricOperation? StartConsumeInstrument<TConsumer, T>(
        this ILogContext logContext,
        ConsumeContext<T> context)
        where T : class =>
        StartProcess(
            logContext,
            context,
            ConsumerProcessorIdentity<TConsumer>.OperationName,
            ConsumerProcessorIdentity<TConsumer>.ProcessorKind);

    public static MetricOperation? StartActivityExecuteInstrument<TActivity, TArguments>(
        this ILogContext logContext,
        ConsumeContext<Courier.Contracts.RoutingSlip> context)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class =>
        StartProcess(logContext, context, "execute", "courier_execute");

    public static MetricOperation? StartActivityCompensateInstrument<TActivity, TLog>(
        this ILogContext logContext,
        ConsumeContext<Courier.Contracts.RoutingSlip> context)
        where TActivity : class, ICompensateActivity<TLog>
        where TLog : class =>
        StartProcess(logContext, context, "compensate", "courier_compensate");

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

    public static MetricOperation? StartOutboxEnqueueInstrument(this ILogContext logContext) =>
        StartOutbox(logContext, "enqueue");

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
            // The provider-specific uninstrumented log context installed above prevents accidental
            // reuse of a previously activated provider's meter scope.
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

    private static MetricOperation? StartProcess(ILogContext logContext, PipeContext context, string operationName, string processorKind) =>
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
                ? SystemName(consume.ReceiveContext.InputAddress)
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

    private static DateTimeOffset AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Local => new DateTimeOffset(value.ToUniversalTime()),
        DateTimeKind.Utc => new DateTimeOffset(value),
        _ => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)),
    };

    private static string SystemName(Uri? address) =>
        NormalizeSystem(address?.Scheme);

    internal static string NormalizeSystem(string? system) => system?.ToLowerInvariant() switch
    {
        "loopback" or "in-memory" => ServiceBusTelemetry.MessagingSystems.InMemory,
        "rabbitmq" => ServiceBusTelemetry.MessagingSystems.RabbitMq,
        "activemq" => ServiceBusTelemetry.MessagingSystems.ActiveMq,
        "amazonsqs" or "amazon_sqs" or "aws-sqs" or "aws_sqs" => ServiceBusTelemetry.MessagingSystems.AmazonSqs,
        "eventhubs" => ServiceBusTelemetry.MessagingSystems.AzureEventHubs,
        "sb" or "azure-service-bus" or "servicebus" => ServiceBusTelemetry.MessagingSystems.AzureServiceBus,
        "db" or "sql" => ServiceBusTelemetry.MessagingSystems.Sql,
        { Length: > 0 } => ServiceBusTelemetry.MessagingSystems.Other,
        _ => ServiceBusTelemetry.MessagingSystems.Unknown,
    };

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
