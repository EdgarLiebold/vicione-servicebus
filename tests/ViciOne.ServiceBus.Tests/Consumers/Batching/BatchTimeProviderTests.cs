using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Batching.Runtime;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Testing;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Consumers.Batching;

public sealed class BatchTimeProviderTests
{
    private static readonly DateTimeOffset StartTime = new(2040, 11, 12, 13, 14, 15, TimeSpan.Zero);
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(5);

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-CLOCK", "from-first-exact-boundary-and-metadata")]
    public async Task FromFirstBatch_ClosesAtTheConfiguredClockBoundaryWithExactMetadataAsync()
    {
        var executorFault = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        LogContext.ConfigureCurrentLogContext(new CapturingLogger(executorFault));
        TimeSpan limit = TimeSpan.FromMinutes(1);
        var clock = new FakeTimeProvider(StartTime);
        var delivered = new TaskCompletionSource<IMessageBatch<BatchItem>>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var collector = new TaskExecutor();
        await using var dispatcher = new TaskExecutor();
        var consumer = new BatchConsumer<BatchItem>(
            new BatchRuntimeSettings(new BatchOptions { MessageLimit = 10, TimeLimit = limit }),
            collector,
            dispatcher,
            new CaptureBatchPipe(delivered),
            clock);

        clock.Advance(TimeSpan.FromSeconds(5));
        await consumer.AddAsync(CreateContext(new BatchItem(1), StartTime.UtcDateTime.AddSeconds(5)), null!, TestContext.Current.CancellationToken);

        clock.Advance(limit - TimeSpan.FromTicks(1));
        Assert.False(delivered.Task.IsCompleted);

        clock.Advance(TimeSpan.FromTicks(1));
        IMessageBatch<BatchItem> batch = await AwaitDeliveryAsync(delivered.Task, executorFault.Task);

        Assert.Equal(BatchCompletionMode.Time, batch.Mode);
        Assert.Equal(StartTime.AddSeconds(5), batch.FirstMessageReceived);
        Assert.Equal(StartTime.AddSeconds(5), batch.LastMessageReceived);
        Assert.Equal(1, Assert.Single(batch).Message.Sequence);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-CLOCK", "from-last-resets-fake-timer")]
    public async Task FromLastBatch_RestartsItsTimerFromTheLatestMessageAsync()
    {
        var executorFault = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        LogContext.ConfigureCurrentLogContext(new CapturingLogger(executorFault));
        TimeSpan limit = TimeSpan.FromMinutes(1);
        var clock = new ObservableTimeProvider(StartTime);
        var delivered = new TaskCompletionSource<IMessageBatch<BatchItem>>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var collector = new TaskExecutor();
        await using var dispatcher = new TaskExecutor();
        var consumer = new BatchConsumer<BatchItem>(
            new BatchRuntimeSettings(new BatchOptions
            {
                MessageLimit = 10,
                TimeLimit = limit,
                TimeLimitStart = BatchTimeLimitStart.FromLast,
            }),
            collector,
            dispatcher,
            new CaptureBatchPipe(delivered),
            clock);

        await consumer.AddAsync(CreateContext(new BatchItem(1), StartTime.UtcDateTime), null!, TestContext.Current.CancellationToken);
        Assert.Equal(1, clock.ChangeCount);
        Assert.Equal(limit, clock.LastDueTime);
        clock.Advance(TimeSpan.FromSeconds(50));
        await consumer.AddAsync(CreateContext(new BatchItem(2), StartTime.UtcDateTime.AddSeconds(50)), null!, TestContext.Current.CancellationToken);
        Assert.Equal(2, clock.ChangeCount);
        Assert.Equal(limit, clock.LastDueTime);

        clock.Advance(limit - TimeSpan.FromTicks(1));
        Assert.False(delivered.Task.IsCompleted);

        clock.Advance(TimeSpan.FromTicks(1));
        IMessageBatch<BatchItem> batch = await AwaitDeliveryAsync(delivered.Task, executorFault.Task);

        Assert.Equal(BatchCompletionMode.Time, batch.Mode);
        Assert.Equal(new[] { 1, 2 }, batch.Select(context => context.Message.Sequence));
        Assert.Equal(StartTime.AddSeconds(50), batch.LastMessageReceived);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-CLOCK", "timer-starts-on-first-unique-admission")]
    public async Task Timer_StartsWithTheFirstUniqueAdmissionAndIgnoresDuplicatesAsync()
    {
        var executorFault = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        LogContext.ConfigureCurrentLogContext(new CapturingLogger(executorFault));
        TimeSpan limit = TimeSpan.FromMinutes(1);
        var clock = new ObservableTimeProvider(StartTime);
        var delivered = new TaskCompletionSource<IMessageBatch<BatchItem>>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var collector = new TaskExecutor();
        await using var dispatcher = new TaskExecutor();
        var consumer = new BatchConsumer<BatchItem>(
            new BatchRuntimeSettings(new BatchOptions
            {
                MessageLimit = 10,
                TimeLimit = limit,
                TimeLimitStart = BatchTimeLimitStart.FromLast,
            }),
            collector,
            dispatcher,
            new CaptureBatchPipe(delivered),
            clock);
        ConsumeContext<BatchItem> first = CreateContext(new BatchItem(1), StartTime.UtcDateTime);

        clock.Advance(TimeSpan.FromHours(1));
        Assert.False(delivered.Task.IsCompleted);
        Assert.Equal(0, clock.ChangeCount);

        await consumer.AddAsync(first, null, TestContext.Current.CancellationToken);
        Assert.Equal(1, clock.ChangeCount);
        Assert.Equal(limit, clock.LastDueTime);

        clock.Advance(TimeSpan.FromSeconds(30));
        await consumer.AddAsync(first, null, TestContext.Current.CancellationToken);
        Assert.Equal(1, clock.ChangeCount);

        clock.Advance(limit - TimeSpan.FromSeconds(30));
        IMessageBatch<BatchItem> batch = await AwaitDeliveryAsync(delivered.Task, executorFault.Task);
        Assert.Equal(StartTime.AddHours(1), batch.FirstMessageReceived);
        Assert.Equal(StartTime.AddHours(1), batch.LastMessageReceived);
        Assert.Equal(1, Assert.Single(batch).Message.Sequence);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-ORDERING", "sent-time-fallback-priority-is-context-transport-then-clock")]
    public async Task OrderingFallback_PrefersContextThenTransportAndFinallyTheConfiguredClockAsync()
    {
        var clock = new FakeTimeProvider(StartTime);
        var delivered = new TaskCompletionSource<IMessageBatch<BatchItem>>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var collector = new TaskExecutor();
        await using var dispatcher = new TaskExecutor();
        var consumer = new BatchConsumer<BatchItem>(
            new BatchRuntimeSettings(new BatchOptions { MessageLimit = 3, TimeLimit = TimeSpan.FromMinutes(1) }),
            collector,
            dispatcher,
            new CaptureBatchPipe(delivered),
            clock);
        ConsumeContext<BatchItem> contextTimestamp = CreateContext(
            new BatchItem(1),
            StartTime.AddTicks(40),
            StartTime.AddTicks(10));
        ConsumeContext<BatchItem> transportTimestamp = CreateContext(
            new BatchItem(2),
            sentTime: null,
            transportSentTime: StartTime.AddTicks(30));

        await consumer.AddAsync(contextTimestamp, null, TestContext.Current.CancellationToken);
        Task contextPipeline = consumer.ConsumeAsync(contextTimestamp);
        await consumer.AddAsync(transportTimestamp, null, TestContext.Current.CancellationToken);
        Task transportPipeline = consumer.ConsumeAsync(transportTimestamp);

        clock.Advance(TimeSpan.FromTicks(20));
        ConsumeContext<BatchItem> clockTimestamp = CreateContext(new BatchItem(3), sentTime: null, transportSentTime: null);
        await consumer.AddAsync(clockTimestamp, null, TestContext.Current.CancellationToken);
        Task clockPipeline = consumer.ConsumeAsync(clockTimestamp);

        await Task.WhenAll(contextPipeline, transportPipeline, clockPipeline)
            .WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);

        IMessageBatch<BatchItem> batch = await delivered.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        Assert.Equal(new[] { 3, 2, 1 }, batch.Select(context => context.Message.Sequence));
    }

    private static ConsumeContext<BatchItem> CreateContext(BatchItem message, DateTime sentTime)
    {
        return CreateContext(message, new DateTimeOffset(sentTime, TimeSpan.Zero), transportSentTime: null);
    }

    private static ConsumeContext<BatchItem> CreateContext(
        BatchItem message,
        DateTimeOffset? sentTime,
        DateTimeOffset? transportSentTime)
    {
        ReceiveContext receiveContext = DispatchProxy.Create<ReceiveContext, ReceiveContextProxy>();
        ((ReceiveContextProxy)(object)receiveContext).Configure(transportSentTime);
        ConsumeContext<BatchItem> context = DispatchProxy.Create<BatchConsumeContext, ConsumeContextProxy>();
        ((ConsumeContextProxy)(object)context).Configure(message, sentTime, receiveContext);
        return context;
    }

    private interface BatchConsumeContext :
        ConsumeContext<BatchItem>,
        ConsumeContext;

    private sealed record BatchItem(int Sequence);

    private sealed class CaptureBatchPipe(TaskCompletionSource<IMessageBatch<BatchItem>> delivered) : IPipe<ConsumeContext<IMessageBatch<BatchItem>>>
    {
        public void Probe(ProbeContext context)
        {
        }

        public Task SendAsync(ConsumeContext<IMessageBatch<BatchItem>> context)
        {
            delivered.TrySetResult(context.Message);
            return Task.CompletedTask;
        }
    }

    private class ConsumeContextProxy : DispatchProxy
    {
        private BatchItem _message = null!;
        private ReceiveContext _receiveContext = null!;
        private SerializerContext _serializerContext = null!;
        private DateTimeOffset? _sentTime;
        private Guid _messageId;

        public void Configure(BatchItem message, DateTimeOffset? sentTime, ReceiveContext receiveContext)
        {
            _message = message;
            _sentTime = sentTime;
            _receiveContext = receiveContext;
            _serializerContext = DispatchProxy.Create<SerializerContext, UnsupportedInvocationProxy>();
            _messageId = NewId.NextGuid();
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            return targetMethod.Name switch
            {
                "get_Message" => _message,
                "get_MessageId" => _messageId,
                "get_SentTime" => _sentTime,
                "get_ReceiveContext" => _receiveContext,
                "get_SerializerContext" => _serializerContext,
                "get_CancellationToken" => CancellationToken.None,
                "HasPayloadType" => false,
                "TryGetPayload" => SetMissingPayload(args),
                _ => throw new NotSupportedException(targetMethod.Name),
            };
        }
    }

    private class ReceiveContextProxy : DispatchProxy
    {
        private static readonly IPublishEndpointProvider PublishEndpointProvider = new UnsupportedPublishEndpointProvider();
        private Headers _transportHeaders = EmptyHeaders.Instance;

        public void Configure(DateTimeOffset? transportSentTime)
        {
            if (transportSentTime.HasValue)
            {
                _transportHeaders = new DictionarySendHeaders(
                [
                    new KeyValuePair<string, object>(MessageHeaders.TransportSentTime, transportSentTime.Value),
                ]);
            }
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            return targetMethod.Name switch
            {
                "get_PublishEndpointProvider" => PublishEndpointProvider,
                "get_TransportHeaders" => _transportHeaders,
                "HasPayloadType" => false,
                "TryGetPayload" => SetMissingPayload(args),
                _ => throw new NotSupportedException(targetMethod.Name),
            };
        }
    }

    private class UnsupportedInvocationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException(targetMethod?.Name);
    }

    private sealed class UnsupportedPublishEndpointProvider : IPublishEndpointProvider
    {
        public Task<ISendEndpoint> GetPublishSendEndpointAsync<T>(CancellationToken cancellationToken = default)
            where T : class => throw new NotSupportedException();

        public ConnectHandle ConnectPublishObserver(IPublishObserver observer) =>
            throw new NotSupportedException();
    }

    private static bool SetMissingPayload(object?[]? args)
    {
        args![0] = null;
        return false;
    }

    private static async Task<IMessageBatch<BatchItem>> AwaitDeliveryAsync(Task<IMessageBatch<BatchItem>> delivery, Task<Exception> executorFault)
    {
        Task completed = await Task.WhenAny(delivery, executorFault)
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        if (completed == executorFault)
            throw await executorFault;

        return await delivery;
    }

    private sealed class CapturingLogger(TaskCompletionSource<Exception> fault) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (exception != null)
                fault.TrySetResult(exception);
        }
    }
}
