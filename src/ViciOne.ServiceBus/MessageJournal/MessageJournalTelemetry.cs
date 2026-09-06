using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Monitoring;

namespace ViciOne.ServiceBus.MessageJournal;

internal static class MessageJournalTelemetry
{
    private static readonly Lazy<Instrumentation> Instruments = new(
        static () => new Instrumentation(),
        LazyThreadSafetyMode.ExecutionAndPublication);

    public static void Stored(MessageJournalOperation operation, MessageJournalOutcome outcome, TimeSpan duration)
    {
        Observe(operation, outcome, "stored", duration);
    }

    public static void Filtered(MessageJournalOperation operation, MessageJournalOutcome outcome, TimeSpan duration)
    {
        Observe(operation, outcome, "filtered", duration);
    }

    public static void Failed(
        MessageJournalOperation operation,
        MessageJournalOutcome outcome,
        string reason,
        TimeSpan duration)
    {
        try
        {
            TagList tags = CreateTags(operation, outcome, "failed");
            tags.Add("message_journal.failure.reason", reason);
            Instruments.Value.Operations.Add(1, tags);
            Instruments.Value.Duration.Record(duration.TotalSeconds, tags);

            using Activity? activity = Instruments.Value.ActivitySource.StartActivity(
                "ViciOne.ServiceBus.MessageJournal.Write",
                ActivityKind.Internal);
            ApplyTags(activity, tags);
        }
        catch (Exception)
        {
            // Telemetry observers never own message or journal semantics.
        }
    }

    private static void Observe(
        MessageJournalOperation operation,
        MessageJournalOutcome outcome,
        string result,
        TimeSpan duration)
    {
        try
        {
            TagList tags = CreateTags(operation, outcome, result);
            Instruments.Value.Operations.Add(1, tags);
            Instruments.Value.Duration.Record(duration.TotalSeconds, tags);

            using Activity? activity = Instruments.Value.ActivitySource.StartActivity(
                "ViciOne.ServiceBus.MessageJournal.Write",
                ActivityKind.Internal);
            ApplyTags(activity, tags);
        }
        catch (Exception)
        {
            // Telemetry observers never own message or journal semantics.
        }
    }

    private static TagList CreateTags(
        MessageJournalOperation operation,
        MessageJournalOutcome outcome,
        string result)
    {
        TagList tags = default;
        tags.Add("message_journal.operation", operation.ToString().ToLowerInvariant());
        tags.Add("message_journal.outcome", outcome.ToString().ToLowerInvariant());
        tags.Add("message_journal.result", result);
        return tags;
    }

    private static void ApplyTags(Activity? activity, TagList tags)
    {
        if (activity is null)
            return;

        foreach (KeyValuePair<string, object?> tag in tags)
            activity.SetTag(tag.Key, tag.Value);
    }

    private sealed class Instrumentation
    {
        private readonly Meter _meter;

        public Instrumentation()
        {
            string? version = HostMetadataCache.Host.ViciOneServiceBusVersion;
            _meter = new Meter(ServiceBusTelemetry.MeterName, version);
            ActivitySource = new ActivitySource(ServiceBusTelemetry.ActivitySourceName, version);
            Operations = _meter.CreateCounter<long>("vicione.servicebus.message_journal.operations");
            Duration = _meter.CreateHistogram<double>(
                "vicione.servicebus.message_journal.duration",
                "s",
                "Time spent projecting and storing a message-journal observation");
        }

        public ActivitySource ActivitySource { get; }

        public Counter<long> Operations { get; }

        public Histogram<double> Duration { get; }
    }
}
