using System.Reflection;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Scheduling;

public sealed class MessageSchedulerRuntimeContractTests
{
    private static readonly Uri Destination = new("loopback://localhost/runtime-scheduled-send");
    private static readonly DateTimeOffset DueAt = new(2044, 5, 6, 7, 8, 9, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-RUNTIME-SCHEDULED-SEND", "declared-contract-preserves-payload-pipe-and-cancellation")]
    public async Task DeclaredContract_PreservesPayloadPipeAndCancellationAsync()
    {
        var provider = new RecordingScheduleProvider();
        IBusTopology topology = DispatchProxy.Create<IBusTopology, UnexpectedTopologyProxy>();
        IAdvancedMessageScheduler scheduler = new MessageScheduler(provider, topology);
        var message = new ScheduledCommand("order-42");
        Guid correlationId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var pipeContexts = new List<SendContext>();
        IPipe<SendContext> pipe = Pipe.Execute<SendContext>(context =>
        {
            context.CorrelationId = correlationId;
            pipeContexts.Add(context);
        });
        using var cancellation = new CancellationTokenSource();

        ScheduledMessage scheduled = await scheduler.ScheduleSendAsync(
            Destination,
            DueAt,
            (object)message,
            typeof(IScheduledCommand),
            pipe,
            cancellation.Token);

        Assert.Equal(1, provider.InvocationCount);
        Assert.Equal(typeof(IScheduledCommand), provider.ContractType);
        Assert.Same(message, provider.Message);
        Assert.Equal(Destination, provider.Destination);
        Assert.Equal(DueAt, provider.DueAt);
        Assert.Equal(cancellation.Token, provider.CancellationToken);
        SendContext context = Assert.Single(pipeContexts);
        Assert.Same(provider.Context, context);
        Assert.Equal(correlationId, context.CorrelationId);
        var handle = Assert.IsType<ScheduledMessageHandle<IScheduledCommand>>(scheduled);
        Assert.Equal(provider.TokenId, handle.TokenId);
        Assert.Equal(Destination, handle.Destination);
        Assert.Equal(DueAt, handle.DueAt);
        Assert.Same(message, handle.Payload);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RUNTIME-SCHEDULED-SEND", "runtime-contract-uses-concrete-payload-type")]
    public async Task RuntimeContract_UsesTheConcretePayloadTypeAsync()
    {
        var provider = new RecordingScheduleProvider();
        IBusTopology topology = DispatchProxy.Create<IBusTopology, UnexpectedTopologyProxy>();
        IAdvancedMessageScheduler scheduler = new MessageScheduler(provider, topology);
        var message = new ScheduledCommand("order-43");
        using var cancellation = new CancellationTokenSource();

        ScheduledMessage scheduled = await scheduler.ScheduleSendAsync(
            Destination,
            DueAt,
            (object)message,
            cancellation.Token);

        Assert.Equal(1, provider.InvocationCount);
        Assert.Equal(typeof(ScheduledCommand), provider.ContractType);
        Assert.Same(message, provider.Message);
        Assert.Equal(Destination, provider.Destination);
        Assert.Equal(DueAt, provider.DueAt);
        Assert.Equal(cancellation.Token, provider.CancellationToken);
        var handle = Assert.IsType<ScheduledMessageHandle<ScheduledCommand>>(scheduled);
        Assert.Equal(provider.TokenId, handle.TokenId);
        Assert.Equal(Destination, handle.Destination);
        Assert.Equal(DueAt, handle.DueAt);
        Assert.Same(message, handle.Payload);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RUNTIME-SCHEDULED-SEND", "runtime-contract-applies-untyped-pipe")]
    public async Task RuntimeContract_AppliesTheUntypedPipeToTheConcreteSendContextAsync()
    {
        var provider = new RecordingScheduleProvider();
        IBusTopology topology = DispatchProxy.Create<IBusTopology, UnexpectedTopologyProxy>();
        IAdvancedMessageScheduler scheduler = new MessageScheduler(provider, topology);
        var message = new ScheduledCommand("order-45");
        Guid requestId = Guid.Parse("30000000-0000-0000-0000-000000000003");
        var pipeContexts = new List<SendContext>();
        IPipe<SendContext> pipe = Pipe.Execute<SendContext>(context =>
        {
            context.RequestId = requestId;
            pipeContexts.Add(context);
        });
        using var cancellation = new CancellationTokenSource();

        ScheduledMessage scheduled = await scheduler.ScheduleSendAsync(
            Destination, DueAt, (object)message, pipe, cancellation.Token);

        Assert.Equal(1, provider.InvocationCount);
        Assert.Equal(typeof(ScheduledCommand), provider.ContractType);
        Assert.Same(message, provider.Message);
        Assert.Equal(Destination, provider.Destination);
        Assert.Equal(DueAt, provider.DueAt);
        Assert.Equal(cancellation.Token, provider.CancellationToken);
        SendContext context = Assert.Single(pipeContexts);
        Assert.Same(provider.Context, context);
        Assert.Equal(requestId, context.RequestId);
        var handle = Assert.IsType<ScheduledMessageHandle<ScheduledCommand>>(scheduled);
        Assert.Equal(provider.TokenId, handle.TokenId);
        Assert.Equal(Destination, handle.Destination);
        Assert.Equal(DueAt, handle.DueAt);
        Assert.Same(message, handle.Payload);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RUNTIME-SCHEDULED-SEND", "declared-contract-without-pipe-retains-interface")]
    public async Task DeclaredContractWithoutPipe_RetainsTheInterfaceContractAsync()
    {
        var provider = new RecordingScheduleProvider();
        IBusTopology topology = DispatchProxy.Create<IBusTopology, UnexpectedTopologyProxy>();
        IAdvancedMessageScheduler scheduler = new MessageScheduler(provider, topology);
        var message = new ScheduledCommand("order-46");
        using var cancellation = new CancellationTokenSource();

        ScheduledMessage scheduled = await scheduler.ScheduleSendAsync(
            Destination, DueAt, (object)message, typeof(IScheduledCommand), cancellation.Token);

        Assert.Equal(1, provider.InvocationCount);
        Assert.Equal(typeof(IScheduledCommand), provider.ContractType);
        Assert.Same(message, provider.Message);
        Assert.Equal(Destination, provider.Destination);
        Assert.Equal(DueAt, provider.DueAt);
        Assert.Equal(cancellation.Token, provider.CancellationToken);
        var handle = Assert.IsType<ScheduledMessageHandle<IScheduledCommand>>(scheduled);
        Assert.Equal(provider.TokenId, handle.TokenId);
        Assert.Equal(Destination, handle.Destination);
        Assert.Equal(DueAt, handle.DueAt);
        Assert.Same(message, handle.Payload);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-RUNTIME-SCHEDULED-SEND", "mismatched-contract-fails-before-provider-and-pipe")]
    public async Task MismatchedContract_FailsBeforeProviderAndPipeAsync(bool withPipe)
    {
        var provider = new RecordingScheduleProvider();
        IBusTopology topology = DispatchProxy.Create<IBusTopology, UnexpectedTopologyProxy>();
        IAdvancedMessageScheduler scheduler = new MessageScheduler(provider, topology);
        var pipeRuns = 0;
        IPipe<SendContext> pipe = Pipe.Execute<SendContext>(_ => pipeRuns++);

        ArgumentException exception = withPipe
            ? await Assert.ThrowsAsync<ArgumentException>(() => scheduler.ScheduleSendAsync(
                Destination, DueAt, new object(), typeof(IScheduledCommand), pipe, TestContext.Current.CancellationToken))
            : await Assert.ThrowsAsync<ArgumentException>(() => scheduler.ScheduleSendAsync(
                Destination, DueAt, new object(), typeof(IScheduledCommand), TestContext.Current.CancellationToken));

        Assert.Contains("Unexpected message type", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, provider.InvocationCount);
        Assert.Equal(0, pipeRuns);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RUNTIME-SCHEDULED-SEND", "invalid-boundary-fails-before-provider")]
    public async Task InvalidBoundary_FailsBeforeProviderDispatchAsync()
    {
        var provider = new RecordingScheduleProvider();
        IBusTopology topology = DispatchProxy.Create<IBusTopology, UnexpectedTopologyProxy>();
        IAdvancedMessageScheduler scheduler = new MessageScheduler(provider, topology);
        var message = new ScheduledCommand("order-44");
        IPipe<SendContext> pipe = Pipe.Empty<SendContext>();

        Assert.Equal("destinationAddress", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            scheduler.ScheduleSendAsync(null!, DueAt, message, typeof(IScheduledCommand), pipe,
                TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("message", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            scheduler.ScheduleSendAsync(Destination, DueAt, null!, typeof(IScheduledCommand), pipe,
                TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("messageType", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            scheduler.ScheduleSendAsync(Destination, DueAt, message, null!, pipe,
                TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("pipe", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            scheduler.ScheduleSendAsync(Destination, DueAt, message, typeof(IScheduledCommand), null!,
                TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("destinationAddress", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            scheduler.ScheduleSendAsync(null!, DueAt, (object)message,
                TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("message", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            scheduler.ScheduleSendAsync(Destination, DueAt, (object)null!,
                TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("destinationAddress", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            scheduler.ScheduleSendAsync(null!, DueAt, (object)message, typeof(IScheduledCommand),
                TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("message", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            scheduler.ScheduleSendAsync(Destination, DueAt, (object)null!, typeof(IScheduledCommand),
                TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("messageType", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            scheduler.ScheduleSendAsync(Destination, DueAt, (object)message, messageType: null!,
                cancellationToken: TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("destinationAddress", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            scheduler.ScheduleSendAsync(null!, DueAt, (object)message, pipe,
                TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("message", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            scheduler.ScheduleSendAsync(Destination, DueAt, (object)null!, pipe,
                TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("pipe", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            scheduler.ScheduleSendAsync(Destination, DueAt, (object)message, pipe: null!,
                cancellationToken: TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal(0, provider.InvocationCount);
    }

    private interface IScheduledCommand
    {
        string Value { get; }
    }

    private sealed record ScheduledCommand(string Value) : IScheduledCommand;

    private sealed class RecordingScheduleProvider : IScheduleMessageProvider
    {
        public Guid TokenId { get; } = Guid.Parse("20000000-0000-0000-0000-000000000002");

        public int InvocationCount { get; private set; }

        public Type? ContractType { get; private set; }

        public Uri? Destination { get; private set; }

        public DateTimeOffset DueAt { get; private set; }

        public object? Message { get; private set; }

        public SendContext? Context { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public async Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destinationAddress, DateTimeOffset dueAt, T message,
            IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
            where T : class
        {
            InvocationCount++;
            ContractType = typeof(T);
            Destination = destinationAddress;
            DueAt = dueAt;
            Message = message;
            CancellationToken = cancellationToken;
            var context = new InMemorySendContext<T>(message, cancellationToken);
            Context = context;

            await pipe.SendAsync(context);

            return new ScheduledMessageHandle<T>(TokenId, dueAt, destinationAddress, message);
        }

        public Task CancelScheduledSendAsync(Guid tokenId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task CancelScheduledSendAsync(Uri destinationAddress, Guid tokenId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private class UnexpectedTopologyProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"An explicit destination must not query topology ({targetMethod?.Name}).");
    }
}
