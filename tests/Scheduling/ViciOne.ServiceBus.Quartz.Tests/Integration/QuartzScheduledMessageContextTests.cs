using System.Text.Json;
using Quartz;
using Quartz.Extensibility;
using Quartz.Impl;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Quartz.Runtime;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Quartz.Tests.Integration;

public sealed class QuartzScheduledMessageContextTests
{
    private static readonly Guid GeneratedMessageSeed = Guid.Parse("018f6738-7d4a-7b21-86e2-bdfbb3ed5f90");
    private static readonly DateTimeOffset FireTime = new(2035, 4, 5, 6, 7, 8, TimeSpan.Zero);
    private static readonly DateTimeOffset DueAt = FireTime.AddMinutes(-1);
    private static readonly DateTimeOffset PreviousTime = DueAt.AddHours(-1);
    private static readonly DateTimeOffset NextTime = DueAt.AddHours(1);

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-JOB-DATA", "serialized-metadata-roundtrip")]
    public void SerializedMetadata_IsRestoredWithExactQuartzHeaders()
    {
        Guid messageId = Guid.Parse("018f6738-7d4a-7b21-86e2-bdfbb3ed5f27");
        var headers = new[]
        {
            new KeyValuePair<string, object>("tenant", "factory-a"),
            new KeyValuePair<string, object>("attempt", 3),
        };
        var properties = new Dictionary<string, object>
        {
            ["partition"] = "north",
            ["priority"] = 7,
        };
        JobExecutionContextImpl execution = CreateExecutionContext(
            "Recurring.Trigger.alpha.Recurring.Trigger.beta",
            new JobDataMap
            {
                ["MessageId"] = messageId.ToString("D"),
                ["Headers"] = JsonSerializer.Serialize(headers, ServiceBusMetadataJson.Options),
                ["TransportProperties"] = JsonSerializer.Serialize(properties, ServiceBusMetadataJson.Options),
                ["SchedulingTokenId"] = "token-42",
                [QuartzJobDataKeys.ScheduleId] = "alpha.Recurring.Trigger.beta",
                [QuartzJobDataKeys.ScheduleGroup] = "operations",
            });

        var context = new QuartzScheduledMessageContext(execution, ServiceBusMetadataJson.ObjectDeserializer);

        Assert.Equal(messageId, context.MessageId);
        Assert.Equal("alpha.Recurring.Trigger.beta", context.Headers.Get<string>(MessageHeaders.Quartz.ScheduleId));
        Assert.Equal("operations", context.Headers.Get<string>(MessageHeaders.Quartz.ScheduleGroup));
        Assert.Equal(FireTime, context.Headers.Get<DateTimeOffset>(MessageHeaders.Quartz.Sent));
        Assert.Equal(DueAt, context.Headers.Get<DateTimeOffset>(MessageHeaders.Quartz.Scheduled));
        Assert.Equal(PreviousTime, context.Headers.Get<DateTimeOffset>(MessageHeaders.Quartz.PreviousSent));
        Assert.Equal(NextTime, context.Headers.Get<DateTimeOffset>(MessageHeaders.Quartz.NextScheduled));
        Assert.Equal("token-42", context.Headers.Get<string>(MessageHeaders.SchedulingTokenId));
        Assert.Equal("factory-a", context.Headers.Get<string>("tenant"));
        Assert.Equal(3, context.Headers.Get<int>("attempt"));

        IReadOnlyDictionary<string, object> restored = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object>>(
            context.TransportProperties);
        Assert.Equal("north", ServiceBusMetadataJson.ObjectDeserializer.DeserializeObject<string>(restored["partition"]));
        Assert.Equal(7, ServiceBusMetadataJson.ObjectDeserializer.DeserializeObject<int>(restored["priority"]));
        Assert.Same(restored, context.TransportProperties);
    }

    [Theory]
    [InlineData(QuartzJobDataKeys.MessageId)]
    [InlineData(QuartzJobDataKeys.RequestId)]
    [InlineData(QuartzJobDataKeys.CorrelationId)]
    [InlineData(QuartzJobDataKeys.ConversationId)]
    [InlineData(QuartzJobDataKeys.InitiatorId)]
    [InlineData(QuartzJobDataKeys.MessageIdSeed)]
    [RequirementCoverage("REQ-VSB-QUARTZ-JOB-DATA", "invalid-identifier")]
    public void InvalidStandardIdentifier_FailsFast(string key)
    {
        JobExecutionContextImpl execution = CreateExecutionContext(
            "single",
            new JobDataMap { [key] = "not-a-guid" });

        FormatException exception = Assert.Throws<FormatException>(() =>
            new QuartzScheduledMessageContext(execution, ServiceBusMetadataJson.ObjectDeserializer));

        Assert.Contains($"'{key}'", exception.Message, StringComparison.Ordinal);
        Assert.Contains("D-format GUID", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-JOB-DATA", "noncanonical-identifier")]
    public void NonCanonicalIdentifier_FailsFast()
    {
        Guid identifier = Guid.Parse("018f6738-7d4a-7b21-86e2-bdfbb3ed5f91");
        JobExecutionContextImpl execution = CreateExecutionContext(
            "single",
            new JobDataMap { [QuartzJobDataKeys.MessageId] = identifier.ToString("N") });

        FormatException exception = Assert.Throws<FormatException>(() =>
            new QuartzScheduledMessageContext(execution, ServiceBusMetadataJson.ObjectDeserializer));

        Assert.Contains($"'{QuartzJobDataKeys.MessageId}'", exception.Message, StringComparison.Ordinal);
        Assert.Contains("D-format GUID", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-JOB-DATA", "generated-identity-is-stable-per-firing")]
    public void MissingMessageIdentifier_IsStableAcrossPersistentRetriesButUniqueAcrossOccurrences()
    {
        JobExecutionContextImpl firstExecution = CreateExecutionContext("single", new JobDataMap());
        JobExecutionContextImpl retryExecution = CreateExecutionContext("single", new JobDataMap());
        JobExecutionContextImpl nextExecution = CreateExecutionContext(
            "single",
            new JobDataMap(),
            scheduledFireTime: DueAt.AddHours(1));

        var firstAttempt = new QuartzScheduledMessageContext(firstExecution, ServiceBusMetadataJson.ObjectDeserializer);
        var retryAttempt = new QuartzScheduledMessageContext(retryExecution, ServiceBusMetadataJson.ObjectDeserializer);
        var nextFiring = new QuartzScheduledMessageContext(nextExecution, ServiceBusMetadataJson.ObjectDeserializer);

        Assert.NotEqual(Guid.Empty, firstAttempt.MessageId);
        Assert.Equal(firstAttempt.MessageId, retryAttempt.MessageId);
        Assert.NotEqual(firstAttempt.MessageId, nextFiring.MessageId);
        Assert.False(firstExecution.MergedJobDataMap.ContainsKey(QuartzJobDataKeys.MessageId));
        Assert.Equal(GeneratedMessageSeed.ToString("D"), firstExecution.MergedJobDataMap.GetString(QuartzJobDataKeys.MessageIdSeed));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-JOB-DATA", "generated-identity-requires-persisted-seed")]
    public void MissingMessageIdentifier_RejectsAMissingPersistentSeed()
    {
        JobExecutionContextImpl execution = CreateExecutionContext(
            "single",
            new JobDataMap(),
            includeMessageIdSeed: false);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            new QuartzScheduledMessageContext(execution, ServiceBusMetadataJson.ObjectDeserializer));

        Assert.Contains($"'{QuartzJobDataKeys.MessageIdSeed}'", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-JOB-DATA", "missing-header-defaults")]
    public void MissingHeader_ReturnsSuppliedReferenceAndValueFallbacks()
    {
        JobExecutionContextImpl execution = CreateExecutionContext("single", new JobDataMap());
        var context = new QuartzScheduledMessageContext(execution, ServiceBusMetadataJson.ObjectDeserializer);

        string? referenceValue = context.Get("missing-reference", "reference-fallback");
        int? valueTypeValue = context.Get<int>("missing-value", 47);

        Assert.Equal("reference-fallback", referenceValue);
        Assert.Equal(47, valueTypeValue);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-JOB-DATA", "present-header-overrides-defaults")]
    public void PresentHeader_OverridesSuppliedReferenceAndValueFallbacks()
    {
        var headers = new[]
        {
            new KeyValuePair<string, object>("reference", "persisted"),
            new KeyValuePair<string, object>("value", 23),
        };
        JobExecutionContextImpl execution = CreateExecutionContext(
            "single",
            new JobDataMap
            {
                ["Headers"] = JsonSerializer.Serialize(headers, ServiceBusMetadataJson.Options),
            });
        var context = new QuartzScheduledMessageContext(execution, ServiceBusMetadataJson.ObjectDeserializer);

        string? referenceValue = context.Get("reference", "reference-fallback");
        int? valueTypeValue = context.Get<int>("value", 47);

        Assert.Equal("persisted", referenceValue);
        Assert.Equal(23, valueTypeValue);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-JOB-DATA", "serialized-user-headers-use-the-header-contract")]
    public void SerializedUserHeaders_AreAvailableThroughEveryHeaderAccessPath()
    {
        var headers = new[]
        {
            new KeyValuePair<string, object>("tenant", "factory-a"),
            new KeyValuePair<string, object>("attempt", 3),
        };
        JobExecutionContextImpl execution = CreateExecutionContext(
            "single",
            new JobDataMap
            {
                ["Headers"] = JsonSerializer.Serialize(headers, ServiceBusMetadataJson.Options),
            });
        var context = new QuartzScheduledMessageContext(execution, ServiceBusMetadataJson.ObjectDeserializer);

        Assert.True(context.TryGetHeader("tenant", out object? tenant));
        Assert.Equal("factory-a", tenant);
        Assert.Equal("factory-a", context.Get<string>("tenant"));
        Assert.Equal(3, context.Get<int>("attempt"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-JOB-DATA", "destination-key-roundtrip")]
    public void PersistedDestination_IsExposedByTheMessageContext()
    {
        var destination = new Uri("loopback://localhost/scheduled-destination");
        JobExecutionContextImpl execution = CreateExecutionContext(
            "single",
            new JobDataMap
            {
                ["DestinationAddress"] = destination.ToString(),
            });

        var context = new QuartzScheduledMessageContext(execution, ServiceBusMetadataJson.ObjectDeserializer);

        Assert.Equal(destination, context.DestinationAddress);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-JOB-DATA", "constructor-null-guards")]
    public void Constructor_RejectsMissingDependencies()
    {
        JobExecutionContextImpl execution = CreateExecutionContext("single", new JobDataMap());

        ArgumentNullException missingContext = Assert.Throws<ArgumentNullException>(() =>
            new QuartzScheduledMessageContext(null!, ServiceBusMetadataJson.ObjectDeserializer));
        ArgumentNullException missingDeserializer = Assert.Throws<ArgumentNullException>(() =>
            new QuartzScheduledMessageContext(execution, null!));

        Assert.Equal("executionContext", missingContext.ParamName);
        Assert.Equal("objectDeserializer", missingDeserializer.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-JOB-DATA", "standard-header-contract")]
    public void StandardMessageProperties_AreAvailableThroughTheHeaderContract()
    {
        Guid messageId = Guid.Parse("018f6738-7d4a-7b21-86e2-bdfbb3ed5f51");
        Guid requestId = Guid.Parse("018f6738-7d4a-7b21-86e2-bdfbb3ed5f52");
        Guid correlationId = Guid.Parse("018f6738-7d4a-7b21-86e2-bdfbb3ed5f53");
        Guid conversationId = Guid.Parse("018f6738-7d4a-7b21-86e2-bdfbb3ed5f54");
        Guid initiatorId = Guid.Parse("018f6738-7d4a-7b21-86e2-bdfbb3ed5f55");
        var source = new Uri("loopback://localhost/source");
        var response = new Uri("loopback://localhost/response");
        var fault = new Uri("loopback://localhost/fault");
        JobExecutionContextImpl execution = CreateExecutionContext(
            "single",
            new JobDataMap
            {
                ["MessageId"] = messageId.ToString(),
                ["RequestId"] = requestId.ToString(),
                ["CorrelationId"] = correlationId.ToString(),
                ["ConversationId"] = conversationId.ToString(),
                ["InitiatorId"] = initiatorId.ToString(),
                ["SourceAddress"] = source.ToString(),
                ["ResponseAddress"] = response.ToString(),
                ["FaultAddress"] = fault.ToString(),
            });
        var context = new QuartzScheduledMessageContext(execution, ServiceBusMetadataJson.ObjectDeserializer);

        AssertHeader(context, MessageHeaders.MessageId, messageId);
        AssertHeader(context, MessageHeaders.RequestId, requestId);
        AssertHeader(context, MessageHeaders.CorrelationId, correlationId);
        AssertHeader(context, MessageHeaders.ConversationId, conversationId);
        AssertHeader(context, MessageHeaders.InitiatorId, initiatorId);
        AssertHeader(context, MessageHeaders.SourceAddress, source);
        AssertHeader(context, MessageHeaders.ResponseAddress, response);
        AssertHeader(context, MessageHeaders.FaultAddress, fault);
        Assert.False(context.TryGetHeader("missing", out object? missing));
        Assert.Null(missing);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-JOB-DATA", "complete-metadata-snapshot")]
    public void MetadataHeadersAndTransportProperties_AreSnapshottedWhenTheContextIsCreated()
    {
        Guid messageId = Guid.Parse("018f6738-7d4a-7b21-86e2-bdfbb3ed5f71");
        Guid requestId = Guid.Parse("018f6738-7d4a-7b21-86e2-bdfbb3ed5f72");
        var expiration = new DateTimeOffset(2034, 5, 6, 7, 8, 9, TimeSpan.Zero);
        var source = new Uri("loopback://localhost/original-source");
        var headers = new[] { new KeyValuePair<string, object>("tenant", "original") };
        var transportProperties = new Dictionary<string, object> { ["partition"] = "north" };
        var data = new JobDataMap
        {
            [QuartzJobDataKeys.MessageId] = messageId.ToString(),
            [QuartzJobDataKeys.RequestId] = requestId.ToString(),
            [QuartzJobDataKeys.ExpirationTime] = expiration.ToString("O"),
            [QuartzJobDataKeys.SourceAddress] = source.ToString(),
            [QuartzJobDataKeys.Headers] = JsonSerializer.Serialize(headers, ServiceBusMetadataJson.Options),
            [QuartzJobDataKeys.TransportProperties] = JsonSerializer.Serialize(transportProperties, ServiceBusMetadataJson.Options),
        };
        JobExecutionContextImpl execution = CreateExecutionContext("single", data);
        var context = new QuartzScheduledMessageContext(execution, ServiceBusMetadataJson.ObjectDeserializer);

        execution.MergedJobDataMap[QuartzJobDataKeys.RequestId] = Guid.NewGuid().ToString();
        execution.MergedJobDataMap[QuartzJobDataKeys.ExpirationTime] = expiration.AddDays(1).ToString("O");
        execution.MergedJobDataMap[QuartzJobDataKeys.SourceAddress] = "loopback://localhost/changed-source";
        execution.MergedJobDataMap[QuartzJobDataKeys.Headers] = JsonSerializer.Serialize(
            new[] { new KeyValuePair<string, object>("tenant", "changed") },
            ServiceBusMetadataJson.Options);
        execution.MergedJobDataMap[QuartzJobDataKeys.TransportProperties] = JsonSerializer.Serialize(
            new Dictionary<string, object> { ["partition"] = "south" },
            ServiceBusMetadataJson.Options);

        Assert.Equal(messageId, context.MessageId);
        Assert.Equal(requestId, context.RequestId);
        Assert.Equal(expiration, context.ExpirationTime);
        Assert.Equal(source, context.SourceAddress);
        Assert.Equal("original", context.Headers.Get<string>("tenant"));
        IReadOnlyDictionary<string, object> capturedProperties = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object>>(
            context.TransportProperties);
        Assert.Equal(
            "north",
            ServiceBusMetadataJson.ObjectDeserializer.DeserializeObject<string>(capturedProperties["partition"]));
    }

    [Theory]
    [InlineData(QuartzJobDataKeys.SourceAddress)]
    [InlineData(QuartzJobDataKeys.DestinationAddress)]
    [InlineData(QuartzJobDataKeys.ResponseAddress)]
    [InlineData(QuartzJobDataKeys.FaultAddress)]
    [RequirementCoverage("REQ-VSB-QUARTZ-JOB-DATA", "absolute-message-addresses")]
    public void RelativeStandardAddress_FailsFast(string key)
    {
        JobExecutionContextImpl execution = CreateExecutionContext(
            "single",
            new JobDataMap { [key] = "relative/address" });

        FormatException exception = Assert.Throws<FormatException>(() =>
            new QuartzScheduledMessageContext(execution, ServiceBusMetadataJson.ObjectDeserializer));

        Assert.Contains($"'{key}'", exception.Message, StringComparison.Ordinal);
        Assert.Contains("absolute URI", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-JOB-DATA", "header-enumeration")]
    public void HeaderEnumeration_ExposesGenericAndNonGenericViews()
    {
        var headers = new[] { new KeyValuePair<string, object>("tenant", "factory-a") };
        JobExecutionContextImpl execution = CreateExecutionContext(
            "single",
            new JobDataMap
            {
                ["Headers"] = JsonSerializer.Serialize(headers, ServiceBusMetadataJson.Options),
            });
        var context = new QuartzScheduledMessageContext(execution, ServiceBusMetadataJson.ObjectDeserializer);

        Assert.Contains(context, header => header.Key == "tenant");
        System.Collections.IEnumerator nonGenericEnumerator =
            ((System.Collections.IEnumerable)context).GetEnumerator();
        Assert.True(nonGenericEnumerator.MoveNext());
        Assert.Equal("tenant", Assert.IsType<HeaderValue>(nonGenericEnumerator.Current).Key);
        Assert.Contains(context.GetAll(), header => header.Key == "tenant");
    }

    [Theory]
    [InlineData(QuartzJobDataKeys.ExpirationTime, "")]
    [InlineData(QuartzJobDataKeys.ExpirationTime, "   ")]
    [InlineData(QuartzJobDataKeys.ExpirationTime, "not-a-timestamp")]
    [InlineData(QuartzJobDataKeys.ExpirationTime, "2034-05-06")]
    [RequirementCoverage("REQ-VSB-QUARTZ-JOB-DATA", "invalid-timestamps-fail-fast")]
    public void InvalidTimestamp_FailsFast(string key, string value)
    {
        JobExecutionContextImpl execution = CreateExecutionContext(
            "single",
            new JobDataMap { [key] = value });

        FormatException exception = Assert.Throws<FormatException>(() =>
            new QuartzScheduledMessageContext(execution, ServiceBusMetadataJson.ObjectDeserializer));

        Assert.Contains($"'{key}'", exception.Message, StringComparison.Ordinal);
        Assert.Contains("round-trip timestamp", exception.Message, StringComparison.Ordinal);
    }

    private static void AssertHeader(QuartzScheduledMessageContext context, string key, object expected)
    {
        Assert.True(context.TryGetHeader(key, out object? actual));
        Assert.Equal(expected, actual);
    }

    private static JobExecutionContextImpl CreateExecutionContext(
        string triggerName,
        JobDataMap data,
        DateTimeOffset? scheduledFireTime = null,
        bool includeMessageIdSeed = true)
    {
        if (includeMessageIdSeed
            && !data.ContainsKey(QuartzJobDataKeys.MessageId)
            && !data.ContainsKey(QuartzJobDataKeys.MessageIdSeed))
        {
            data[QuartzJobDataKeys.MessageIdSeed] = GeneratedMessageSeed.ToString("D");
        }

        DateTimeOffset scheduledTime = scheduledFireTime ?? DueAt;
        IJobDetail job = JobBuilder.Create<NoOpJob>()
            .WithIdentity("scheduled-message-job")
            .StoreDurably()
            .Build();
        IOperableTrigger trigger = (IOperableTrigger)TriggerBuilder.Create()
            .WithIdentity(triggerName)
            .ForJob(job.Key)
            .UsingJobData(data)
            .StartAt(scheduledTime)
            .Build();
        var bundle = new TriggerFiredBundle
        {
            JobDetail = job,
            Trigger = trigger,
            Calendar = null,
            Recovering = false,
            FireTimeUtc = FireTime,
            ScheduledFireTimeUtc = scheduledTime,
            PreviousFireTimeUtc = PreviousTime,
            NextFireTimeUtc = NextTime,
        };

        return new JobExecutionContextImpl(null!, bundle, new NoOpJob());
    }

    private sealed class NoOpJob : global::Quartz.IJob
    {
        public ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            return cancellationToken.IsCancellationRequested
                ? ValueTask.FromCanceled(cancellationToken)
                : ValueTask.CompletedTask;
        }
    }
}
