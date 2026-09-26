using System.Reflection;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.JobService;
using ViciOne.ServiceBus.JobService.Messages;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.JobService.JobService;

public sealed class SubmitJobConsumerTests
{
    static readonly DateTimeOffset Start = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("missing-start-and-cron", "CronExpression")]
    [InlineData("reversed-window", "Start")]
    [InlineData("invalid-cron", "CronExpression")]
    [InlineData("blank-time-zone", "TimeZoneId")]
    [RequirementCoverage("REQ-VSB-JOB-SUBMISSION-API", "invalid-schedules-rejected-before-serialization-and-coordination")]
    public async Task InvalidSchedule_RejectsSubmissionBeforeSerializingOrPublishingAsync(string scenario, string expectedKey)
    {
        JobScheduleInfo schedule = scenario switch
        {
            "missing-start-and-cron" => new(),
            "reversed-window" => new() { Start = Start, End = Start.AddTicks(-1) },
            "invalid-cron" => new() { CronExpression = "not-a-cron-expression" },
            "blank-time-zone" => new() { Start = Start, TimeZoneId = " " },
            _ => throw new ArgumentOutOfRangeException(nameof(scenario)),
        };
        var outgoing = new List<object>();
        SerializerContext serializer = CreateSerializer(out SerializerProbe probe);
        var message = new SubmitJobCommand<TestJob>
        {
            JobId = Guid.NewGuid(),
            Job = new TestJob("invoice-42"),
            Schedule = schedule,
        };
        ConsumeContext<ISubmitJob<TestJob>> context = CreateContext(message, serializer, outgoing);
        var consumer = new SubmitJobConsumer<TestJob>(new JobOptions<TestJob>(), Guid.NewGuid());

        ConfigurationException failure = await Assert.ThrowsAsync<ConfigurationException>(() => consumer.ConsumeAsync(context));

        Assert.Contains(expectedKey, failure.Message, StringComparison.Ordinal);
        Assert.Equal(0, probe.DictionaryCalls);
        Assert.Empty(outgoing);
    }

    [Theory]
    [InlineData("start-only")]
    [InlineData("equal-window")]
    [InlineData("cron-only")]
    [RequirementCoverage("REQ-VSB-JOB-SUBMISSION-API", "valid-schedule-boundaries-preserve-complete-coordination-command")]
    public async Task ValidSchedule_PublishesOneCompleteCoordinationCommandAsync(string scenario)
    {
        JobScheduleInfo schedule = scenario switch
        {
            "start-only" => new() { Start = Start },
            "equal-window" => new() { Start = Start, End = Start },
            "cron-only" => new() { CronExpression = "0 0 12 * * ?", TimeZoneId = "UTC" },
            _ => throw new ArgumentOutOfRangeException(nameof(scenario)),
        };
        var outgoing = new List<object>();
        SerializerContext serializer = CreateSerializer(out SerializerProbe probe);
        var message = new SubmitJobCommand<TestJob>
        {
            JobId = Guid.NewGuid(),
            Job = new TestJob("invoice-42"),
            Schedule = schedule,
            JobProperties = new Dictionary<string, object> { ["priority"] = 7 },
        };
        Guid jobTypeId = Guid.NewGuid();
        var options = new JobOptions<TestJob> { JobTimeout = TimeSpan.FromMinutes(7) };
        ConsumeContext<ISubmitJob<TestJob>> context = CreateContext(message, serializer, outgoing);
        var consumer = new SubmitJobConsumer<TestJob>(options, jobTypeId);

        await consumer.ConsumeAsync(context);

        IJobSubmitted submitted = Assert.IsAssignableFrom<IJobSubmitted>(Assert.Single(outgoing));
        Assert.Equal(message.JobId, submitted.JobId);
        Assert.Equal(jobTypeId, submitted.JobTypeId);
        Assert.Equal(Start.AddDays(-1), submitted.Timestamp);
        Assert.Equal(TimeSpan.FromMinutes(7), submitted.JobTimeout);
        Assert.Same(message.JobProperties, submitted.JobProperties);
        Assert.Equal(1, probe.DictionaryCalls);
        Assert.Same(message.Job, probe.Input);
        Assert.Same(probe.Serialized, submitted.Job);
        Assert.NotNull(submitted.Schedule);
        Assert.NotSame(schedule, submitted.Schedule);
        Assert.Equal(schedule.Start, submitted.Schedule.Start);
        Assert.Equal(schedule.End, submitted.Schedule.End);
        Assert.Equal(schedule.CronExpression, submitted.Schedule.CronExpression);
        Assert.Equal(schedule.TimeZoneId, submitted.Schedule.TimeZoneId);
    }

    static ConsumeContext<ISubmitJob<TestJob>> CreateContext(
        ISubmitJob<TestJob> message, SerializerContext serializer, List<object> outgoing)
    {
        SubmissionContext context = DispatchProxy.Create<SubmissionContext, SubmissionProbe>();
        var probe = (SubmissionProbe)(object)context;
        probe.Inner = InMemoryOutboxTestContextFactory.Create(
            message, TestContext.Current.CancellationToken, sentTime: Start.AddDays(-1), serializerContext: serializer).Advanced();
        probe.Outgoing = outgoing;
        return context;
    }

    public interface SubmissionContext : ConsumeContext<ISubmitJob<TestJob>>, ConsumeContext;

    class SubmissionProbe : DispatchProxy
    {
        public ConsumeContext Inner { get; set; } = null!;
        public List<object> Outgoing { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            if (targetMethod.Name != "PublishAsync")
                return targetMethod.Invoke(Inner, args);
            ((CancellationToken)args![1]!).ThrowIfCancellationRequested();
            Outgoing.Add(args[0]!);
            return Task.CompletedTask;
        }
    }

    static SerializerContext CreateSerializer(out SerializerProbe probe)
    {
        SerializerContext serializer = DispatchProxy.Create<SerializerContext, SerializerProbe>();
        probe = (SerializerProbe)(object)serializer;
        probe.Fallback = InMemoryOutboxTestContextFactory.Create(new TestJob("serializer-probe")).Advanced().SerializerContext;
        return serializer;
    }

    class SerializerProbe : DispatchProxy
    {
        public SerializerContext Fallback { get; set; } = null!;
        public int DictionaryCalls { get; private set; }
        public object? Input { get; private set; }
        public Dictionary<string, object> Serialized { get; } = new() { ["name"] = "invoice-42" };

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            if (targetMethod.Name != nameof(SerializerContext.ToDictionary))
                return targetMethod.Invoke(Fallback, args);
            DictionaryCalls++;
            Input = args![0];
            return Serialized;
        }
    }

    public sealed record TestJob(string Name);
}
