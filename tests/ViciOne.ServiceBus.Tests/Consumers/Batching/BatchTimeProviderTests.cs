using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Batching;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Testing;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Consumers.Batching;

public sealed class BatchTimeProviderTests
{
    private static readonly DateTimeOffset StartTime = new(2040, 11, 12, 13, 14, 15, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-CLOCK", "from-first-exact-boundary-and-metadata")]
    public async Task FromFirstBatch_ClosesAtTheConfiguredClockBoundaryWithExactMetadataAsync()
    {
        var executorFault = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        LogContext.ConfigureCurrentLogContext(new CapturingLogger(executorFault));
        TimeSpan limit = TimeSpan.FromMinutes(1);
        var clock = new FakeTimeProvider(StartTime);
        var delivered = new TaskCompletionSource<Batch<BatchItem>>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var collector = new TaskExecutor();
        await using var dispatcher = new TaskExecutor();
        var consumer = new BatchConsumer<BatchItem>(
            new BatchOptions { MessageLimit = 10, TimeLimit = limit },
            collector,
            dispatcher,
            new CaptureBatchPipe(delivered),
            clock);

        clock.Advance(TimeSpan.FromSeconds(5));
        await consumer.AddAsync(CreateContext(new BatchItem(1), StartTime.UtcDateTime.AddSeconds(5)), null!, TestContext.Current.CancellationToken);

        clock.Advance(limit - TimeSpan.FromSeconds(5) - TimeSpan.FromTicks(1));
        Assert.False(delivered.Task.IsCompleted);

        clock.Advance(TimeSpan.FromTicks(1));
        Batch<BatchItem> batch = await AwaitDeliveryAsync(delivered.Task, executorFault.Task);

        Assert.Equal(BatchCompletionMode.Time, batch.Mode);
        Assert.Equal(StartTime.UtcDateTime, batch.FirstMessageReceived);
        Assert.Equal(StartTime.UtcDateTime.AddSeconds(5), batch.LastMessageReceived);
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
        var delivered = new TaskCompletionSource<Batch<BatchItem>>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var collector = new TaskExecutor();
        await using var dispatcher = new TaskExecutor();
        var consumer = new BatchConsumer<BatchItem>(
            new BatchOptions
            {
                MessageLimit = 10,
                TimeLimit = limit,
                TimeLimitStart = BatchTimeLimitStart.FromLast,
            },
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
        Batch<BatchItem> batch = await AwaitDeliveryAsync(delivered.Task, executorFault.Task);

        Assert.Equal(BatchCompletionMode.Time, batch.Mode);
        Assert.Equal(new[] { 1, 2 }, batch.Select(context => context.Message.Sequence));
        Assert.Equal(StartTime.UtcDateTime.AddSeconds(50), batch.LastMessageReceived);
    }

    private static ConsumeContext<BatchItem> CreateContext(BatchItem message, DateTime sentTime)
    {
        ReceiveContext receiveContext = DispatchProxy.Create<ReceiveContext, ReceiveContextProxy>();
        ConsumeContext<BatchItem> context = DispatchProxy.Create<BatchConsumeContext, ConsumeContextProxy>();
        ((ConsumeContextProxy)(object)context).Configure(message, sentTime, receiveContext);
        return context;
    }

    private interface BatchConsumeContext :
        ConsumeContext<BatchItem>,
        ConsumeContext;

    private sealed record BatchItem(int Sequence);

    private sealed class CaptureBatchPipe(TaskCompletionSource<Batch<BatchItem>> delivered) : IPipe<ConsumeContext<Batch<BatchItem>>>
    {
        public void Probe(ProbeContext context)
        {
        }

        public Task SendAsync(ConsumeContext<Batch<BatchItem>> context)
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
        private DateTimeOffset _sentTime;
        private Guid _messageId;

        public void Configure(BatchItem message, DateTime sentTime, ReceiveContext receiveContext)
        {
            _message = message;
            _sentTime = new DateTimeOffset(sentTime, TimeSpan.Zero);
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

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            return targetMethod.Name switch
            {
                "get_PublishEndpointProvider" => PublishEndpointProvider,
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

    private static async Task<Batch<BatchItem>> AwaitDeliveryAsync(Task<Batch<BatchItem>> delivery, Task<Exception> executorFault)
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
