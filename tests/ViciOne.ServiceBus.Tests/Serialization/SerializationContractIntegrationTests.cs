using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.Events;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.InMemoryTransport;
using ViciOne.ServiceBus.JobService.Messages;
using ViciOne.ServiceBus.MessageData;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class SerializationContractIntegrationTests
{
    private static readonly TimeSpan RedeliveryInterval = TimeSpan.FromHours(1);

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-CHALLENGING-CONTRACTS", "complete-shape-set")]
    public async Task SystemTextJson_ChallengingContractsRoundTripEveryValueAsync()
    {
        byte[] bytes = [0x56, 0x34, 0xF3];
        var binary = SystemTextJsonRoundTrip.Execute(new BinaryMessage { Contents = bytes });
        InitializeContext<BinaryContract> initialized = await MessageInitializerCache<BinaryContract>.InitializeAsync(
            new { Contents = bytes },
            TestContext.Current.CancellationToken);
        BinaryContract binaryContract = SystemTextJsonRoundTrip.Execute(initialized.Message);
        var temporalSource = new TemporalMessage
        {
            Local = new DateTime(2001, 9, 11, 8, 46, 30, DateTimeKind.Local),
            Universal = new DateTime(2001, 9, 11, 13, 3, 2, DateTimeKind.Utc),
        };
        TemporalMessage temporal = SystemTextJsonRoundTrip.Execute(temporalSource);
        ConstructorBoundMessage constructorBound = SystemTextJsonRoundTrip.Execute(
            new ConstructorBoundMessage("Dru", "Sellers"));
        var scalarSource = new ScalarMessage
        {
            DecimalValue = 123.45m,
            LongValue = 98_123_213,
            BoolValue = true,
            ByteValue = 127,
            IntValue = 123,
            DateTimeValue = new DateTime(2008, 9, 8, 7, 6, 5, 4, DateTimeKind.Utc),
            TimeSpanValue = TimeSpan.FromSeconds(30),
            GuidValue = Guid.Parse("bc69335e-9329-4d14-88fb-f9dd06331461"),
            StringValue = "complete",
            DoubleValue = 1823.172,
            OptionalDecimal = 567.89m,
        };
        ScalarMessage scalar = SystemTextJsonRoundTrip.Execute(scalarSource);
        EmptyMessage empty = SystemTextJsonRoundTrip.Execute(new EmptyMessage());
        PrecisionMessage precision = SystemTextJsonRoundTrip.Execute(
            new PrecisionMessage { Value = 0.000001m });

        Assert.Equal(bytes, binary.Contents);
        Assert.Equal(bytes, binaryContract.Contents);
        Assert.NotSame(bytes, binary.Contents);
        Assert.NotSame(bytes, binaryContract.Contents);
        Assert.Equal(temporalSource.Local, temporal.Local);
        Assert.Equal(DateTimeKind.Local, temporal.Local.Kind);
        Assert.Equal(temporalSource.Universal, temporal.Universal);
        Assert.Equal(DateTimeKind.Utc, temporal.Universal.Kind);
        Assert.Equal("Dru", constructorBound.Name);
        Assert.Equal("Sellers", constructorBound.Value);
        AssertScalarMessage(scalarSource, scalar);
        Assert.IsType<EmptyMessage>(empty);
        Assert.Equal(0.000001m, precision.Value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-INTERFACES", "proxy-and-in-memory-dispatch")]
    public async Task SystemTextJson_InterfaceContractDispatchesEveryAccessorShapeAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var received = new TaskCompletionSource<ConsumeContext<ComplaintContract>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration =>
                configuration.AddHandler<ComplaintContract>(context =>
                {
                    received.TrySetResult(context);
                    return Task.CompletedTask;
                }))
            .BuildServiceProvider(validateScopes: true);
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
        InitializeContext<ComplaintContract> initialized = await MessageInitializerCache<ComplaintContract>.InitializeAsync(
            new
            {
                Id = 27,
                AddedBy = new { Name = "Chris", Email = "chris@example.test" },
                AddedAt = new DateTime(2026, 8, 30, 7, 15, 0, DateTimeKind.Utc),
                Subject = "complete",
                Body = "Every interface accessor survives.",
                Area = ComplaintArea.Appearance,
            },
            cancellationToken);

        bool stopped = false;
        try
        {
            await harness.Bus.PublishAsync(initialized.Message, cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            ConsumeContext<ComplaintContract> context = await received.Task.WaitAsync(timeout, cancellationToken);

            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
            stopped = true;

            Assert.Equal(27, context.Message.Id);
            Assert.Equal("Chris", context.Message.AddedBy.Name);
            Assert.Equal("chris@example.test", context.Message.AddedBy.Email);
            Assert.Equal(new DateTime(2026, 8, 30, 7, 15, 0, DateTimeKind.Utc), context.Message.AddedAt);
            Assert.Equal("complete", context.Message.Subject);
            Assert.Equal("Every interface accessor survives.", context.Message.Body);
            Assert.Equal(ComplaintArea.Appearance, context.Message.Area);
            Assert.NotSame(initialized.Message, context.Message);
            Assert.Single(harness.Consumed.Select<ComplaintContract>(SnapshotOnlyToken()));
        }
        finally
        {
            if (!stopped)
                await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-JOBS", "nested-interface-job-payload")]
    public async Task SystemTextJson_JobPayloadRestoresNestedInterfaceListAsync()
    {
        InitializeContext<ConvertVideo> initialized = await MessageInitializerCache<ConvertVideo>.InitializeAsync(
            new
            {
                Path = "input.mp4",
                GroupId = "group-1",
                Index = 0,
                Count = 1,
                Details = new[] { new { Value = "first" }, new { Value = "second" } },
            },
            TestContext.Current.CancellationToken);
        SystemTextJsonRoundTripResult<ConvertVideo> jobRoundTrip =
            SystemTextJsonRoundTrip.ExecuteWithContext(initialized.Message);
        StartJob command = new StartJobCommand
        {
            JobId = NewId.NextGuid(),
            AttemptId = NewId.NextGuid(),
            Job = jobRoundTrip.Context.ToDictionary(jobRoundTrip.Message),
            JobTypeId = NewId.NextGuid(),
        };

        SystemTextJsonRoundTripResult<StartJob> commandRoundTrip =
            SystemTextJsonRoundTrip.ExecuteWithContext(command);
        ConvertVideo? restored = commandRoundTrip.Context.DeserializeObject<ConvertVideo>(
            commandRoundTrip.Message.Job);

        Assert.NotNull(restored);
        Assert.Equal("input.mp4", restored.Path);
        Assert.Equal("group-1", restored.GroupId);
        Assert.Equal(0, restored.Index);
        Assert.Equal(1, restored.Count);
        Assert.Equal(["first", "second"], restored.Details.Select(detail => detail.Value));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-MESSAGE-DATA", "external-reference")]
    public async Task SystemTextJson_MessageDataPreservesExternalReferenceAsync()
    {
        var repository = new InMemoryMessageDataRepository();
        var source = new MessageDataContainer
        {
            Value = await repository.PutStringAsync(
                new string('*', MessageDataPolicy.Default.Threshold + 100),
                TestContext.Current.CancellationToken),
        };

        MessageDataContainer result = SystemTextJsonRoundTrip.Execute(source);

        Assert.NotNull(result.Value);
        Assert.Equal(source.Value.Address, result.Value.Address);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-FAULTS", "serializable-and-nonserializable-exceptions")]
    public void SystemTextJson_ReceiveFaultPreservesExceptionInformation(bool useSerializableException)
    {
        Exception exception = useSerializableException
            ? new InvalidOperationException("serializable failure")
            : new NonSerializableException("nonserializable failure");
        var source = new ReceiveFaultEvent(
            HostMetadataCache.Host,
            new AggregateException(exception),
            "application/test",
            Guid.Parse("01c0db64-d13e-41be-bb1c-2d41c8cd49e8"),
            ["urn:message:Faulted"]);
        ReceiveFault contract = source;

        SystemTextJsonRoundTripResult<ReceiveFault> result =
            SystemTextJsonRoundTrip.ExecuteWithContext(contract);

        Assert.NotEmpty(result.Bytes);
        Assert.Equal(source.FaultId, result.Message.FaultId);
        Assert.Equal(source.FaultedMessageId, result.Message.FaultedMessageId);
        Assert.Equal("application/test", result.Message.ContentType);
        Assert.Equal(["urn:message:Faulted"], result.Message.FaultMessageTypes);
        ExceptionInfo restored = Assert.Single(result.Message.Exceptions);
        Assert.Contains("failure", restored.Message, StringComparison.Ordinal);
        Assert.Contains(exception.GetType().FullName!, restored.ExceptionType, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-RAW-REDELIVERY", "type-id-and-message-id")]
    public async Task RawSystemTextJson_RedeliveryPreservesTypeAndReplacesMessageIdAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var first = new TaskCompletionSource<RawDelivery>(TaskCreationOptions.RunContinuationsAsynchronously);
        var second = new TaskCompletionSource<RawDelivery>(TaskCreationOptions.RunContinuationsAsynchronously);
        var completed = new TaskCompletionSource<ConsumeContext<RawRetryCompleted>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var deliveryCount = 0;
        var completedCount = 0;
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddHandler<RawRetryMessage>(async context =>
                {
                    var delivery = RawDelivery.From(context);
                    if (Interlocked.Increment(ref deliveryCount) == 1)
                    {
                        first.TrySetResult(delivery);
                        throw new ExpectedRawRedeliveryException();
                    }

                    second.TrySetResult(delivery);
                    await context.Advanced().PublishAsync(new RawRetryCompleted(context.Message.Value), context.CancellationToken);
                });
                configuration.AddHandler<RawRetryCompleted>(context =>
                {
                    Interlocked.Increment(ref completedCount);
                    completed.TrySetResult(context);
                    return Task.CompletedTask;
                });
                configuration.AddConfigureEndpointsCallback((_, _, endpoint) =>
                    endpoint.UseDelayedRedelivery(redelivery =>
                    {
                        redelivery.ReplaceMessageId = true;
                        redelivery.Intervals(RedeliveryInterval);
                    }));
                configuration.UsingInMemory((context, transport) =>
                {
                    transport.ClearSerialization();
                    transport.UseRawJsonSerializer(RawSerializerOptions.All);
                    transport.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(validateScopes: true);
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
        var scheduled = new RawScheduledObserver();
        using ConnectHandle observerHandle = harness.Bus.ConnectSendObserver(scheduled);
        Guid originalMessageId = Guid.Parse("568c2bd0-68be-4ef5-9ee5-3ee80629af43");
        RawDelivery firstDelivery;
        RawDelivery secondDelivery;
        ConsumeContext<RawRetryCompleted> completion;
        bool stopped = false;

        try
        {
            await harness.Bus.PublishAsync(
                    new RawRetryMessage("preserved"),
                    context => context.MessageId = originalMessageId,
                    cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            firstDelivery = await first.Task.WaitAsync(timeout, cancellationToken);
            SendContext scheduledContext = await scheduled.Scheduled.WaitAsync(timeout, cancellationToken);

            Assert.Equal(RedeliveryInterval, scheduledContext.Delay);
            provider.GetRequiredService<IInMemoryDelayProvider>().Advance(RedeliveryInterval);

            secondDelivery = await second.Task.WaitAsync(timeout, cancellationToken);
            completion = await completed.Task.WaitAsync(timeout, cancellationToken);
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
            stopped = true;
        }
        finally
        {
            if (!stopped)
                await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }

        string messageUrn = MessageUrn.ForTypeString<RawRetryMessage>();
        Assert.Equal(2, Volatile.Read(ref deliveryCount));
        Assert.Equal(1, Volatile.Read(ref completedCount));
        Assert.Equal("preserved", completion.Message.Value);
        Assert.Equal(originalMessageId, firstDelivery.MessageId);
        Assert.NotNull(secondDelivery.MessageId);
        Assert.NotEqual(firstDelivery.MessageId, secondDelivery.MessageId);
        Assert.Equal(0, firstDelivery.RedeliveryCount);
        Assert.Equal(1, secondDelivery.RedeliveryCount);
        Assert.Equal("application/json", firstDelivery.ContentType);
        Assert.Equal("application/json", secondDelivery.ContentType);
        Assert.Contains(messageUrn, firstDelivery.SupportedMessageTypes, StringComparer.Ordinal);
        Assert.Contains(messageUrn, secondDelivery.SupportedMessageTypes, StringComparer.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-RAW-INTERFACES", "concrete-to-requested-interface")]
    public async Task RawSystemTextJson_ConcreteMessageDispatchesAsRequestedInterfaceAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var received = new TaskCompletionSource<ConsumeContext<RawCommandContract>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var receiveCount = 0;
        using var harness = new InMemoryTestHarness($"raw-interface-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        harness.BeginTestScope();
        harness.OnConfigureInMemoryBus += transport =>
        {
            transport.ClearSerialization();
            transport.UseRawJsonSerializer(RawSerializerOptions.All);
        };
        harness.OnConfigureInMemoryReceiveEndpoint += endpoint =>
            endpoint.Handler<RawCommandContract>(context =>
            {
                Interlocked.Increment(ref receiveCount);
                received.TrySetResult(context);
                return Task.CompletedTask;
            });
        var source = new RawCommand(
            Guid.Parse("c23f0e97-9f0b-49e1-8fd1-e3d74bb522db"),
            "27");
        ConsumeContext<RawCommandContract> context;

        try
        {
            await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
            await harness.InputQueueSendEndpoint.SendAsync(source, cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            context = await received.Task.WaitAsync(timeout, cancellationToken);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Equal(1, Volatile.Read(ref receiveCount));
        Assert.Equal(source.CommandId, context.Message.CommandId);
        Assert.Equal(source.ItemNumber, context.Message.ItemNumber);
        Assert.Equal("application/json", context.Advanced().ReceiveContext.ContentType.MediaType);
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static CancellationToken SnapshotOnlyToken() => new(canceled: true);

    private static void AssertScalarMessage(ScalarMessage expected, ScalarMessage actual)
    {
        Assert.Equal(expected.DecimalValue, actual.DecimalValue);
        Assert.Equal(expected.LongValue, actual.LongValue);
        Assert.Equal(expected.BoolValue, actual.BoolValue);
        Assert.Equal(expected.ByteValue, actual.ByteValue);
        Assert.Equal(expected.IntValue, actual.IntValue);
        Assert.Equal(expected.DateTimeValue, actual.DateTimeValue);
        Assert.Equal(expected.TimeSpanValue, actual.TimeSpanValue);
        Assert.Equal(expected.GuidValue, actual.GuidValue);
        Assert.Equal(expected.StringValue, actual.StringValue);
        Assert.Equal(expected.DoubleValue, actual.DoubleValue);
        Assert.Equal(expected.OptionalDecimal, actual.OptionalDecimal);
    }

    public sealed class BinaryMessage
    {
        public byte[] Contents { get; set; } = [];
    }

    public interface BinaryContract
    {
        byte[] Contents { get; set; }
    }

    public sealed class TemporalMessage
    {
        public DateTime Local { get; set; }
        public DateTime Universal { get; set; }
    }

    public sealed class ConstructorBoundMessage
    {
        public ConstructorBoundMessage(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; private set; }
        public string Value { get; private set; }
    }

    public sealed class ScalarMessage
    {
        public decimal DecimalValue { get; set; }
        public long LongValue { get; set; }
        public bool BoolValue { get; set; }
        public byte ByteValue { get; set; }
        public int IntValue { get; set; }
        public DateTime DateTimeValue { get; set; }
        public TimeSpan TimeSpanValue { get; set; }
        public Guid GuidValue { get; set; }
        public string StringValue { get; set; } = string.Empty;
        public double DoubleValue { get; set; }
        public decimal? OptionalDecimal { get; set; }
    }

    public sealed class EmptyMessage;

    public sealed class PrecisionMessage
    {
        public decimal Value { get; set; }
    }

    public interface ComplaintContract
    {
        int Id { get; init; }
        ComplaintUser AddedBy { get; }
        DateTime AddedAt { get; }
        string Subject { get; set; }
        string Body { get; }
        ComplaintArea Area { get; }
    }

    public interface ComplaintUser
    {
        string Name { get; }
        string Email { get; }
    }

    public enum ComplaintArea
    {
        Unknown,
        Appearance,
        Courtesy,
    }

    public interface VideoDetail
    {
        string Value { get; set; }
    }

    public interface ConvertVideo
    {
        string GroupId { get; }
        int Index { get; }
        int Count { get; }
        string Path { get; }
        IList<VideoDetail> Details { get; }
    }

    public sealed class MessageDataContainer
    {
        public MessageData<string> Value { get; set; } = null!;
    }

    public sealed record RawRetryMessage(string Value);
    public sealed record RawRetryCompleted(string Value);

    public interface RawCommandContract
    {
        Guid CommandId { get; }
        string ItemNumber { get; }
    }

    public sealed record RawCommand(Guid CommandId, string ItemNumber);

    private sealed class NonSerializableException(string message) : Exception(message);
    private sealed class ExpectedRawRedeliveryException : Exception;

    private sealed record RawDelivery(
        Guid? MessageId,
        int RedeliveryCount,
        string ContentType,
        string[] SupportedMessageTypes)
    {
        public static RawDelivery From(ConsumeContext<RawRetryMessage> context) => new(
            context.MessageId,
            context.Advanced().GetRedeliveryCount(),
            context.Advanced().ReceiveContext.ContentType.MediaType,
            [.. context.Advanced().SupportedMessageTypes]);
    }

    private sealed class RawScheduledObserver : ISendObserver
    {
        private readonly TaskCompletionSource<SendContext> _scheduled = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<SendContext> Scheduled => _scheduled.Task;

        public Task PreSendAsync<T>(SendContext<T> context)
            where T : class => Task.CompletedTask;

        public Task PostSendAsync<T>(SendContext<T> context)
            where T : class
        {
            if (typeof(T) == typeof(RawRetryMessage) && context.Delay.HasValue)
                _scheduled.TrySetResult(context);

            return Task.CompletedTask;
        }

        public Task SendFaultAsync<T>(SendContext<T> context, Exception exception)
            where T : class
        {
            if (typeof(T) == typeof(RawRetryMessage) && context.Delay.HasValue)
                _scheduled.TrySetException(exception);

            return Task.CompletedTask;
        }
    }
}
