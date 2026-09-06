using System;
using System.Diagnostics.Metrics;
using ViciOne.ServiceBus.Monitoring;

namespace ViciOne.ServiceBus.Logging;

internal sealed class LogContextInstrumentationState
{
    private static readonly InstrumentAdvice<double> MessagingDurationAdvice = new()
    {
        HistogramBucketBoundaries = Array.AsReadOnly<double>(
            [0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10]),
    };

    public LogContextInstrumentationState(Meter meter, ILogContext rootLogContext)
    {
        Meter = meter;
        RootLogContext = rootLogContext;

        SentMessages = meter.CreateCounter<long>(
            ServiceBusTelemetry.Metrics.SentMessages,
            "{message}",
            "Number of messages the producer attempted to send to a broker.");
        ConsumedMessages = meter.CreateCounter<long>(
            ServiceBusTelemetry.Metrics.ConsumedMessages,
            "{message}",
            "Number of messages delivered to the consumer pipeline.");
        ClientOperationDuration = meter.CreateHistogram<double>(
            ServiceBusTelemetry.Metrics.ClientOperationDuration,
            "s",
            "Duration of a messaging operation initiated by a producer or consumer client.",
            advice: MessagingDurationAdvice);
        ProcessDuration = meter.CreateHistogram<double>(
            ServiceBusTelemetry.Metrics.ProcessDuration,
            "s",
            "Duration of processing a message.",
            advice: MessagingDurationAdvice);
        ActiveOperations = meter.CreateUpDownCounter<long>(
            ServiceBusTelemetry.Metrics.ActiveOperations,
            "{operation}",
            "Number of messaging operations currently in progress.");
        RetryAttempts = meter.CreateCounter<long>(
            ServiceBusTelemetry.Metrics.RetryAttempts,
            "{attempt}",
            "Number of message-processing retry attempts.");
        DeliveryDuration = meter.CreateHistogram<double>(
            ServiceBusTelemetry.Metrics.DeliveryDuration,
            "s",
            "Elapsed time from the message send timestamp to consumer processing.",
            advice: MessagingDurationAdvice);
        OutboxMessages = meter.CreateCounter<long>(
            ServiceBusTelemetry.Metrics.OutboxMessages,
            "{message}",
            "Number of messages processed by the in-memory or persistent outbox boundary.");
    }

    public Meter Meter { get; }
    public ILogContext RootLogContext { get; }
    public Counter<long> SentMessages { get; }
    public Counter<long> ConsumedMessages { get; }
    public Histogram<double> ClientOperationDuration { get; }
    public Histogram<double> ProcessDuration { get; }
    public UpDownCounter<long> ActiveOperations { get; }
    public Counter<long> RetryAttempts { get; }
    public Histogram<double> DeliveryDuration { get; }
    public Counter<long> OutboxMessages { get; }
}
