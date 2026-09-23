using System.Reflection;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Scheduling;

public sealed class MessageSchedulerInitializedContractTests
{
    private static readonly Uri Destination = new("loopback://localhost/initialized-scheduled-send");
    private static readonly DateTimeOffset DueAt = new(2045, 6, 7, 8, 9, 10, TimeSpan.Zero);

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [RequirementCoverage("REQ-VSB-INITIALIZED-SCHEDULED-SEND", "values-create-message-and-compose-selected-pipe")]
    public async Task Values_CreateTheScheduledMessageAndApplyTheSelectedPipeAsync(int pipeKind)
    {
        var provider = new RecordingScheduleProvider();
        IBusTopology topology = DispatchProxy.Create<IBusTopology, UnexpectedTopologyProxy>();
        IAdvancedMessageScheduler scheduler = new MessageScheduler(provider, topology);
        var values = new { Value = "order-47", Count = 37, __Header_Tenant_Code = "north" };
        Guid correlationId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        Guid requestId = Guid.Parse("20000000-0000-0000-0000-000000000002");
        var typedContexts = new List<SendContext<InitializedCommand>>();
        var untypedContexts = new List<SendContext>();
        IPipe<SendContext<InitializedCommand>> typedPipe = Pipe.Execute<SendContext<InitializedCommand>>(context =>
        {
            context.CorrelationId = correlationId;
            typedContexts.Add(context);
        });
        IPipe<SendContext> untypedPipe = Pipe.Execute<SendContext>(context =>
        {
            context.RequestId = requestId;
            untypedContexts.Add(context);
        });
        using var cancellation = new CancellationTokenSource();

        ScheduledMessage<InitializedCommand> scheduled = pipeKind switch
        {
            0 => await scheduler.ScheduleSendAsync<InitializedCommand>(Destination, DueAt, values, cancellation.Token),
            1 => await scheduler.ScheduleSendAsync(Destination, DueAt, values, typedPipe, cancellation.Token),
            2 => await scheduler.ScheduleSendAsync<InitializedCommand>(Destination, DueAt, values, untypedPipe, cancellation.Token),
            _ => throw new ArgumentOutOfRangeException(nameof(pipeKind)),
        };

        Assert.Equal(1, provider.InvocationCount);
        Assert.Equal(typeof(InitializedCommand), provider.ContractType);
        Assert.Equal(Destination, provider.Destination);
        Assert.Equal(DueAt, provider.DueAt);
        Assert.Equal(cancellation.Token, provider.CancellationToken);
        var message = Assert.IsType<InitializedCommand>(provider.Message);
        Assert.Equal("order-47", message.Value);
        Assert.Equal(37, message.Count);
        var context = Assert.IsType<InMemorySendContext<InitializedCommand>>(provider.Context);
        Assert.Same(message, context.Message);
        Assert.Equal("north", context.Headers.Get<string>("Tenant-Code"));
        Assert.Equal(pipeKind == 1 ? correlationId : null, context.CorrelationId);
        Assert.Equal(pipeKind == 2 ? requestId : null, context.RequestId);
        if (pipeKind == 1)
            Assert.Same(context, Assert.Single(typedContexts));
        else
            Assert.Empty(typedContexts);
        if (pipeKind == 2)
            Assert.Same(context, Assert.Single(untypedContexts));
        else
            Assert.Empty(untypedContexts);
        var handle = Assert.IsType<ScheduledMessageHandle<InitializedCommand>>(scheduled);
        Assert.Equal(provider.TokenId, handle.TokenId);
        Assert.Equal(Destination, handle.Destination);
        Assert.Equal(DueAt, handle.DueAt);
        Assert.Same(message, handle.Payload);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZED-SCHEDULED-SEND", "pending-value-delays-provider-dispatch")]
    public async Task PendingValue_DelaysProviderDispatchUntilInitializationCompletesAsync()
    {
        var provider = new RecordingScheduleProvider();
        IBusTopology topology = DispatchProxy.Create<IBusTopology, UnexpectedTopologyProxy>();
        IAdvancedMessageScheduler scheduler = new MessageScheduler(provider, topology);
        var gate = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellation = new CancellationTokenSource();

        Task<ScheduledMessage<InitializedCommand>> operation = scheduler.ScheduleSendAsync<InitializedCommand>(
            Destination, DueAt, new { Value = gate.Task, Count = 41 }, cancellation.Token);

        Assert.False(operation.IsCompleted);
        Assert.Equal(0, provider.InvocationCount);
        gate.SetResult("order-48");

        ScheduledMessage<InitializedCommand> scheduled = await operation;
        Assert.Equal(1, provider.InvocationCount);
        Assert.Equal(cancellation.Token, provider.CancellationToken);
        var message = Assert.IsType<InitializedCommand>(provider.Message);
        Assert.Equal("order-48", message.Value);
        Assert.Equal(41, message.Count);
        Assert.Same(message, Assert.IsType<ScheduledMessageHandle<InitializedCommand>>(scheduled).Payload);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZED-SCHEDULED-SEND", "cancel-pending-value-before-provider-dispatch")]
    public async Task CancellationOfPendingValue_NeverDispatchesToTheProviderAsync()
    {
        var provider = new RecordingScheduleProvider();
        IBusTopology topology = DispatchProxy.Create<IBusTopology, UnexpectedTopologyProxy>();
        IAdvancedMessageScheduler scheduler = new MessageScheduler(provider, topology);
        var gate = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellation = new CancellationTokenSource();

        Task<ScheduledMessage<InitializedCommand>> operation = scheduler.ScheduleSendAsync<InitializedCommand>(
            Destination, DueAt, new { Value = gate.Task, Count = 43 }, cancellation.Token);

        Assert.False(operation.IsCompleted);
        Assert.Equal(0, provider.InvocationCount);
        cancellation.Cancel();

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            operation.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.False(gate.Task.IsCompleted);
        Assert.Equal(0, provider.InvocationCount);
        gate.SetResult("cleanup");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZED-SCHEDULED-SEND", "required-arguments-fail-before-initialization-and-provider")]
    public async Task RequiredArguments_FailBeforeInitializationAndProviderDispatchAsync()
    {
        var provider = new RecordingScheduleProvider();
        IBusTopology topology = DispatchProxy.Create<IBusTopology, UnexpectedTopologyProxy>();
        IAdvancedMessageScheduler scheduler = new MessageScheduler(provider, topology);
        var values = new ObservedValues();
        IPipe<SendContext<InitializedCommand>> typedPipe = Pipe.Empty<SendContext<InitializedCommand>>();
        IPipe<SendContext> untypedPipe = Pipe.Empty<SendContext>();
        using var cancellation = new CancellationTokenSource();

        Assert.Equal("destinationAddress", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            scheduler.ScheduleSendAsync<InitializedCommand>(null!, DueAt, values, cancellation.Token))).ParamName);
        Assert.Equal("values", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            scheduler.ScheduleSendAsync<InitializedCommand>(Destination, DueAt, (object)null!, cancellation.Token))).ParamName);
        Assert.Equal("destinationAddress", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            scheduler.ScheduleSendAsync(null!, DueAt, values, typedPipe, cancellation.Token))).ParamName);
        Assert.Equal("values", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            scheduler.ScheduleSendAsync(Destination, DueAt, (object)null!, typedPipe, cancellation.Token))).ParamName);
        Assert.Equal("pipe", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            scheduler.ScheduleSendAsync<InitializedCommand>(Destination, DueAt, values,
                (IPipe<SendContext<InitializedCommand>>)null!, cancellation.Token))).ParamName);
        Assert.Equal("destinationAddress", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            scheduler.ScheduleSendAsync<InitializedCommand>(null!, DueAt, values, untypedPipe,
                cancellation.Token))).ParamName);
        Assert.Equal("values", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            scheduler.ScheduleSendAsync<InitializedCommand>(Destination, DueAt, (object)null!, untypedPipe,
                cancellation.Token))).ParamName);
        Assert.Equal("pipe", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            scheduler.ScheduleSendAsync<InitializedCommand>(Destination, DueAt, values,
                (IPipe<SendContext>)null!, cancellation.Token))).ParamName);
        Assert.Equal(0, values.ValueReads);
        Assert.Equal(0, provider.InvocationCount);
    }

    public sealed class ObservedValues
    {
        public int ValueReads { get; private set; }

        public string Value
        {
            get
            {
                ValueReads++;
                return "order-49";
            }
        }

        public int Count => 47;
    }

    public sealed class InitializedCommand
    {
        public string? Value { get; set; }

        public int Count { get; set; }
    }

    private sealed class RecordingScheduleProvider : IScheduleMessageProvider
    {
        public Guid TokenId { get; } = Guid.Parse("30000000-0000-0000-0000-000000000003");

        public int InvocationCount { get; private set; }

        public Type? ContractType { get; private set; }

        public Uri? Destination { get; private set; }

        public DateTimeOffset DueAt { get; private set; }

        public object? Message { get; private set; }

        public SendContext? Context { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public async Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destinationAddress, DateTimeOffset dueAt,
            T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
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
