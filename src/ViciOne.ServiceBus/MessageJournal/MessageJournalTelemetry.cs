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
    private const string FailedResult = "failed";
    private const string FilteredResult = "filtered";
    private const string StoredResult = "stored";

    private static readonly Lazy<Instrumentation> Instruments = new(
        static () => new Instrumentation(),
        LazyThreadSafetyMode.ExecutionAndPublication);

    public static Activity? StartActivity(
        MessageJournalOperation operation,
        MessageJournalOutcome outcome)
    {
        try
        {
            TagList tags = CreateTags(operation, outcome, result: null);
            System.Diagnostics.ActivityContext parentContext = Activity.Current?.Context ?? default;
            return Instruments.Value.ActivitySource.StartActivity(
                ServiceBusTelemetry.Activities.MessageJournalObserve,
                ActivityKind.Internal,
                parentContext,
                tags);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static void Stored(
        Activity? activity,
        MessageJournalOperation operation,
        MessageJournalOutcome outcome,
        TimeSpan duration)
    {
        Complete(activity, operation, outcome, StoredResult, failureReason: null, duration);
    }

    public static void Filtered(
        Activity? activity,
        MessageJournalOperation operation,
        MessageJournalOutcome outcome,
        TimeSpan duration)
    {
        Complete(activity, operation, outcome, FilteredResult, failureReason: null, duration);
    }

    public static void Failed(
        Activity? activity,
        MessageJournalOperation operation,
        MessageJournalOutcome outcome,
        string reason,
        TimeSpan duration)
    {
        Complete(activity, operation, outcome, FailedResult, reason, duration);
    }

    private static void Complete(
        Activity? activity,
        MessageJournalOperation operation,
        MessageJournalOutcome outcome,
        string result,
        string? failureReason,
        TimeSpan duration)
    {
        try
        {
            TagList tags = CreateTags(operation, outcome, result);
            if (failureReason is not null)
                tags.Add(ServiceBusTelemetry.Attributes.MessageJournalFailureReason, failureReason);

            ApplyTags(activity, tags);
            activity?.SetStatus(result == FailedResult ? ActivityStatusCode.Error : ActivityStatusCode.Ok, failureReason);
            Instruments.Value.Operations.Add(1, tags);
            Instruments.Value.Duration.Record(duration.TotalSeconds, tags);
        }
        catch (Exception)
        {
            // Telemetry observers never own message or journal semantics.
        }
        finally
        {
            try
            {
                activity?.Dispose();
            }
            catch (Exception)
            {
                // Telemetry observers never own message or journal semantics.
            }
        }
    }

    private static TagList CreateTags(
        MessageJournalOperation operation,
        MessageJournalOutcome outcome,
        string? result)
    {
        TagList tags = default;
        tags.Add(ServiceBusTelemetry.Attributes.MessageJournalOperation, operation.ToString().ToLowerInvariant());
        tags.Add(ServiceBusTelemetry.Attributes.MessageJournalOutcome, outcome.ToString().ToLowerInvariant());
        if (result is not null)
            tags.Add(ServiceBusTelemetry.Attributes.MessageJournalResult, result);

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
            Operations = _meter.CreateCounter<long>(
                ServiceBusTelemetry.Metrics.MessageJournalOperations,
                "{operation}",
                "Completed message-journal observations.");
            Duration = _meter.CreateHistogram<double>(
                ServiceBusTelemetry.Metrics.MessageJournalDuration,
                "s",
                "Duration of processing one message-journal observation.");
        }

        public ActivitySource ActivitySource { get; }

        public Counter<long> Operations { get; }

        public Histogram<double> Duration { get; }
    }
}
