using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Serialization;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.JobService;
using ViciOne.ServiceBus.JobService.Messages;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.JobService.JobService;

public sealed class StartJobConsumerTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-START-CONSUMER", "matching-command-deserializes-and-forwards-every-admission-input")]
    public async Task MatchingCommand_DeserializesAndForwardsEveryAdmissionInputAsync()
    {
        Guid jobTypeId = NewId.NextGuid();
        var job = new TestJob("invoice-42");
        var serializedJob = new Dictionary<string, object> { ["name"] = job.Name };
        var message = new StartJobCommand
        {
            JobId = NewId.NextGuid(),
            AttemptId = NewId.NextGuid(),
            JobTypeId = jobTypeId,
            Job = serializedJob,
        };
        using var cancellation = new CancellationTokenSource();
        var serializer = CreateSerializer(job);
        ConsumeContext<StartJob> context = InMemoryOutboxTestContextFactory.Create<StartJob>(
            message,
            cancellation.Token,
            serializerContext: serializer);
        var service = new RecordingJobService();
        var options = new JobOptions<TestJob>();
        IPipe<ConsumeContext<TestJob>> pipe = Pipe.Empty<ConsumeContext<TestJob>>();
        var consumer = new StartJobConsumer<TestJob>(service, options, jobTypeId, pipe);

        await consumer.ConsumeAsync(context);

        Assert.Equal(1, service.StartCount);
        Assert.Same(context, service.Context);
        Assert.Same(job, service.Job);
        Assert.Same(pipe, service.Pipe);
        Assert.Same(options, service.Options);
        Assert.Equal(cancellation.Token, service.CancellationToken);
        var serializerProxy = (SerializerContextProxy)(object)serializer;
        Assert.Same(serializedJob, serializerProxy.SerializedValue);
        Assert.Equal(typeof(TestJob), serializerProxy.RequestedType);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-START-CONSUMER", "foreign-job-type-does-not-deserialize-or-admit")]
    public async Task ForeignJobType_IsIgnoredBeforeDeserializationAsync()
    {
        Guid registeredTypeId = NewId.NextGuid();
        var message = new StartJobCommand
        {
            JobId = NewId.NextGuid(),
            AttemptId = NewId.NextGuid(),
            JobTypeId = NewId.NextGuid(),
            Job = new Dictionary<string, object>(),
        };
        SerializerContext serializer = CreateSerializer(new TestJob("must-not-be-used"), failWhenInvoked: true);
        ConsumeContext<StartJob> context = InMemoryOutboxTestContextFactory.Create<StartJob>(
            message,
            TestContext.Current.CancellationToken,
            serializerContext: serializer);
        var service = new RecordingJobService();
        var consumer = new StartJobConsumer<TestJob>(
            service,
            new JobOptions<TestJob>(),
            registeredTypeId,
            Pipe.Empty<ConsumeContext<TestJob>>());

        await consumer.ConsumeAsync(context);

        Assert.Equal(0, service.StartCount);
        Assert.Null(((SerializerContextProxy)(object)serializer).RequestedType);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-START-CONSUMER", "null-deserialization-fails-before-admission")]
    public async Task MissingDeserializedJob_FailsBeforeAdmissionAsync()
    {
        Guid jobTypeId = NewId.NextGuid();
        var message = new StartJobCommand
        {
            JobId = NewId.NextGuid(),
            AttemptId = NewId.NextGuid(),
            JobTypeId = jobTypeId,
            Job = new Dictionary<string, object>(),
        };
        ConsumeContext<StartJob> context = InMemoryOutboxTestContextFactory.Create<StartJob>(
            message,
            TestContext.Current.CancellationToken,
            serializerContext: CreateSerializer(null));
        var service = new RecordingJobService();
        var consumer = new StartJobConsumer<TestJob>(
            service,
            new JobOptions<TestJob>(),
            jobTypeId,
            Pipe.Empty<ConsumeContext<TestJob>>());

        SerializationException exception = await Assert.ThrowsAsync<SerializationException>(() => consumer.ConsumeAsync(context));

        Assert.Contains(TypeCache<TestJob>.ShortName, exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, service.StartCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-START-CONSUMER", "all-required-construction-and-consume-inputs")]
    public async Task Consumer_ValidatesEveryRequiredInputAsync()
    {
        var service = new RecordingJobService();
        var options = new JobOptions<TestJob>();
        IPipe<ConsumeContext<TestJob>> pipe = Pipe.Empty<ConsumeContext<TestJob>>();
        Guid jobTypeId = NewId.NextGuid();

        Assert.Equal("jobService", Assert.Throws<ArgumentNullException>(() =>
            new StartJobConsumer<TestJob>(null!, options, jobTypeId, pipe)).ParamName);
        Assert.Equal("options", Assert.Throws<ArgumentNullException>(() =>
            new StartJobConsumer<TestJob>(service, null!, jobTypeId, pipe)).ParamName);
        Assert.Equal("jobTypeId", Assert.Throws<ArgumentException>(() =>
            new StartJobConsumer<TestJob>(service, options, Guid.Empty, pipe)).ParamName);
        Assert.Equal("jobPipe", Assert.Throws<ArgumentNullException>(() =>
            new StartJobConsumer<TestJob>(service, options, jobTypeId, null!)).ParamName);

        var consumer = new StartJobConsumer<TestJob>(service, options, jobTypeId, pipe);
        Assert.Equal(
            "context",
            (await Assert.ThrowsAsync<ArgumentNullException>(() => consumer.ConsumeAsync(null!))).ParamName);
    }

    private static SerializerContext CreateSerializer(TestJob? result, bool failWhenInvoked = false)
    {
        SerializerContext serializer = DispatchProxy.Create<SerializerContext, SerializerContextProxy>();
        var proxy = (SerializerContextProxy)(object)serializer;
        proxy.Result = result;
        proxy.FailWhenInvoked = failWhenInvoked;
        return serializer;
    }

    private class SerializerContextProxy : DispatchProxy
    {
        public TestJob? Result { get; set; }

        public bool FailWhenInvoked { get; set; }

        public object? SerializedValue { get; private set; }

        public Type? RequestedType { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name != nameof(IObjectDeserializer.DeserializeObject))
                throw new NotSupportedException(targetMethod?.Name);
            if (FailWhenInvoked)
                throw new InvalidOperationException("Deserialization must not occur for another job type.");

            SerializedValue = args![0];
            RequestedType = targetMethod.GetGenericArguments()[0];
            return Result;
        }
    }

    private sealed class RecordingJobService : IJobService
    {
        public int StartCount { get; private set; }

        public ConsumeContext<StartJob>? Context { get; private set; }

        public object? Job { get; private set; }

        public object? Pipe { get; private set; }

        public object? Options { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public Uri InstanceAddress => throw new NotSupportedException();

        public JobServiceSettings Settings => throw new NotSupportedException();

        public Task StartJobAsync<TJob>(
            ConsumeContext<StartJob> context,
            TJob job,
            IPipe<ConsumeContext<TJob>> jobPipe,
            JobOptions<TJob> jobOptions,
            CancellationToken cancellationToken = default)
            where TJob : class
        {
            StartCount++;
            Context = context;
            Job = job;
            Pipe = jobPipe;
            Options = jobOptions;
            CancellationToken = cancellationToken;
            return Task.CompletedTask;
        }

        public Task StopAsync(IPublishEndpoint publishEndpoint, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public bool TryGetJob(Guid jobId, [NotNullWhen(true)] out JobHandle? jobReference) =>
            throw new NotSupportedException();

        public bool TryRemoveJob(Guid jobId, [NotNullWhen(true)] out JobHandle? jobHandle) =>
            throw new NotSupportedException();

        public void RegisterJobType<TJob>(JobOptions<TJob> options, Guid jobTypeId, string jobTypeName)
            where TJob : class => throw new NotSupportedException();

        public Task BusStartedAsync(IPublishEndpoint publishEndpoint, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Guid GetJobTypeId<TJob>()
            where TJob : class => throw new NotSupportedException();

        public void ConfigureSuperviseJobConsumer(IReceiveEndpointConfigurator configurator) =>
            throw new NotSupportedException();
    }

    private sealed record TestJob(string Name);
}
