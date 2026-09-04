using System.Text.Json;
using Quartz;
using Quartz.Extensibility;
using Quartz.Impl;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.QuartzIntegration.Tests.QuartzIntegration;

public sealed class JobDataMessageContextTests
{
    private static readonly DateTimeOffset FireTime = new(2035, 4, 5, 6, 7, 8, TimeSpan.Zero);
    private static readonly DateTimeOffset ScheduledTime = FireTime.AddMinutes(-1);
    private static readonly DateTimeOffset PreviousTime = ScheduledTime.AddHours(-1);
    private static readonly DateTimeOffset NextTime = ScheduledTime.AddHours(1);

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
                ["HeadersAsJson"] = JsonSerializer.Serialize(headers, ServiceBusMetadataJson.Options),
                ["TransportProperties"] = JsonSerializer.Serialize(properties, ServiceBusMetadataJson.Options),
                ["TokenId"] = "token-42",
            });

        var context = new JobDataMessageContext(execution, ServiceBusMetadataJson.ObjectDeserializer);

        Assert.Equal(messageId, context.MessageId);
        Assert.Equal("alpha.Recurring.Trigger.beta", context.Headers.Get<string>(MessageHeaders.Quartz.ScheduleId));
        Assert.Equal("DEFAULT", context.Headers.Get<string>(MessageHeaders.Quartz.ScheduleGroup));
        Assert.Equal(FireTime, context.Headers.Get<DateTimeOffset>(MessageHeaders.Quartz.Sent));
        Assert.Equal(ScheduledTime, context.Headers.Get<DateTimeOffset>(MessageHeaders.Quartz.Scheduled));
        Assert.Equal(PreviousTime, context.Headers.Get<DateTimeOffset>(MessageHeaders.Quartz.PreviousSent));
        Assert.Equal(NextTime, context.Headers.Get<DateTimeOffset>(MessageHeaders.Quartz.NextScheduled));
        Assert.Equal("token-42", context.Headers.Get<string>(MessageHeaders.SchedulingTokenId));
        Assert.Equal("factory-a", context.Headers.Get<string>("tenant"));
        Assert.Equal(3, context.Headers.Get<int>("attempt"));

        IReadOnlyDictionary<string, object> restored = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object>>(
            context.TransportProperties);
        Assert.Equal("north", ServiceBusMetadataJson.ObjectDeserializer.DeserializeObject<string>(restored["partition"]));
        Assert.Equal(7, ServiceBusMetadataJson.ObjectDeserializer.DeserializeObject<int>(restored["priority"]));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-JOB-DATA", "invalid-identifier")]
    public void InvalidMessageIdentifier_FailsFast()
    {
        JobExecutionContextImpl execution = CreateExecutionContext(
            "single",
            new JobDataMap { ["MessageId"] = "not-a-guid" });

        FormatException exception = Assert.Throws<FormatException>(() =>
            new JobDataMessageContext(execution, ServiceBusMetadataJson.ObjectDeserializer));

        Assert.Equal("The Id was not a Guid: not-a-guid", exception.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-JOB-DATA", "generated-identity-stability")]
    public void MissingMessageIdentifier_GeneratesOneStableIdentityAndTimestamp()
    {
        JobExecutionContextImpl execution = CreateExecutionContext("single", new JobDataMap());

        var context = new JobDataMessageContext(execution, ServiceBusMetadataJson.ObjectDeserializer);
        Guid? first = context.MessageId;
        Guid? second = context.MessageId;
        DateTime? firstSentTime = context.SentTime;

        Assert.NotNull(first);
        Assert.Equal(first, second);
        Assert.NotNull(firstSentTime);
        Assert.Equal(firstSentTime, context.SentTime);
    }

    private static JobExecutionContextImpl CreateExecutionContext(string triggerName, JobDataMap data)
    {
        IJobDetail job = JobBuilder.Create<NoOpJob>()
            .WithIdentity("scheduled-message-job")
            .StoreDurably()
            .Build();
        IOperableTrigger trigger = (IOperableTrigger)TriggerBuilder.Create()
            .WithIdentity(triggerName)
            .ForJob(job.Key)
            .UsingJobData(data)
            .StartAt(ScheduledTime)
            .Build();
        var bundle = new TriggerFiredBundle
        {
            JobDetail = job,
            Trigger = trigger,
            Calendar = null,
            Recovering = false,
            FireTimeUtc = FireTime,
            ScheduledFireTimeUtc = ScheduledTime,
            PreviousFireTimeUtc = PreviousTime,
            NextFireTimeUtc = NextTime,
        };

        return new JobExecutionContextImpl(null!, bundle, new NoOpJob());
    }

    private sealed class NoOpJob : IJob
    {
        public ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }
}
