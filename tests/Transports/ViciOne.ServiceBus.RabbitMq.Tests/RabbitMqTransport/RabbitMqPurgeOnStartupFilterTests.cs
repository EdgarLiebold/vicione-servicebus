using System.Collections.Concurrent;
using System.Reflection;
using RabbitMQ.Client;
using ViciOne.ServiceBus.RabbitMq.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.RabbitMqTransport;

public sealed class RabbitMqPurgeOnStartupFilterTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-STARTUP-PURGE", "concurrent-channel-pipelines-purge-once")]
    public async Task ConcurrentChannelPipelines_PurgeExactlyOnceBeforeEitherContinuesAsync()
    {
        var broker = new RecordingBroker { PurgeCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        var filter = new PurgeOnStartupFilter("orders");
        var next = new RecordingPipe();
        ChannelContext firstChannel = broker.Channel(TestContext.Current.CancellationToken);
        ChannelContext secondChannel = broker.Channel(TestContext.Current.CancellationToken);

        Task first = filter.SendAsync(firstChannel, next);
        await broker.PurgeEntered.Task.WaitAsync(TestContext.Current.CancellationToken);
        Task second = filter.SendAsync(secondChannel, next);

        try
        {
            Assert.Equal(1, broker.PurgeCalls);
            Assert.False(first.IsCompleted);
            Assert.False(second.IsCompleted);
            Assert.Equal(0, next.Calls);
            Assert.Equal("orders", broker.PurgedQueue);
            Assert.Equal(TestContext.Current.CancellationToken, broker.PurgeToken);
        }
        finally
        {
            broker.PurgeCompletion.TrySetResult(3);
        }
        await Task.WhenAll(first, second).WaitAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, broker.PurgeCalls);
        Assert.Equal(2, next.Calls);
        Assert.Single(next.Contexts, context => ReferenceEquals(context, firstChannel));
        Assert.Single(next.Contexts, context => ReferenceEquals(context, secondChannel));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-STARTUP-PURGE", "empty-snapshot-waits-for-inflight-purge")]
    public async Task EmptySecondSnapshot_CannotAdvanceBeforeTheFirstPurgeFinishesAsync()
    {
        var broker = new RecordingBroker { PurgeCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        broker.Snapshots.Enqueue(new QueueDeclareOk("orders", 3, 0));
        broker.Snapshots.Enqueue(new QueueDeclareOk("orders", 0, 0));
        var filter = new PurgeOnStartupFilter("orders");
        var next = new RecordingPipe();

        Task first = filter.SendAsync(broker.Channel(TestContext.Current.CancellationToken), next);
        await broker.PurgeEntered.Task.WaitAsync(TestContext.Current.CancellationToken);
        Task second = filter.SendAsync(broker.Channel(TestContext.Current.CancellationToken), next);
        try
        {
            Assert.False(second.IsCompleted);
            Assert.Equal(1, broker.PassiveDeclareCalls);
            Assert.Equal(0, next.Calls);
        }
        finally
        {
            broker.PurgeCompletion.TrySetResult(3);
        }

        await Task.WhenAll(first, second).WaitAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, broker.PassiveDeclareCalls);
        Assert.Equal(1, broker.PurgeCalls);
        Assert.Equal(2, next.Calls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-STARTUP-PURGE", "canceled-waiter-does-not-interrupt-owner")]
    public async Task CanceledSecondChannel_DoesNotInterruptTheOwnerOrEnterThePipelineAsync()
    {
        var broker = new RecordingBroker { PurgeCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        var filter = new PurgeOnStartupFilter("orders");
        var next = new RecordingPipe();
        using var waiting = new CancellationTokenSource();

        Task first = filter.SendAsync(broker.Channel(TestContext.Current.CancellationToken), next);
        await broker.PurgeEntered.Task.WaitAsync(TestContext.Current.CancellationToken);
        Task second = filter.SendAsync(broker.Channel(waiting.Token), next);
        waiting.Cancel();

        try
        {
            OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second);
            Assert.Equal(waiting.Token, actual.CancellationToken);
            Assert.Equal(1, broker.PurgeCalls);
            Assert.Equal(0, next.Calls);
        }
        finally
        {
            broker.PurgeCompletion.TrySetResult(3);
        }

        await first.WaitAsync(TestContext.Current.CancellationToken);
        await filter.SendAsync(broker.Channel(TestContext.Current.CancellationToken), next);
        Assert.Equal(1, broker.PurgeCalls);
        Assert.Equal(2, next.Calls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-STARTUP-PURGE", "failed-purge-retries-before-pipeline")]
    public async Task FailedPurge_IsRetriedByNextChannelAndNeverAdmitsItEarlyAsync()
    {
        var broker = new RecordingBroker { FirstPurgeFailure = new InvalidOperationException("broker purge failed") };
        var filter = new PurgeOnStartupFilter("orders");
        var next = new RecordingPipe();
        ChannelContext channel = broker.Channel(TestContext.Current.CancellationToken);

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() => filter.SendAsync(channel, next));
        Assert.Same(broker.FirstPurgeFailure, actual);
        Assert.Equal(0, next.Calls);

        await filter.SendAsync(channel, next);
        await filter.SendAsync(channel, next);

        Assert.Equal(2, broker.PurgeCalls);
        Assert.Equal(2, next.Calls);
        Assert.Equal(3, broker.PassiveDeclareCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-STARTUP-PURGE", "only-nonempty-consumer-free-queue-is-purged")]
    public async Task EmptyOrConsumedQueue_IsNotPurgedAndCanBePurgedAtNextEligibleStartAsync()
    {
        var broker = new RecordingBroker();
        broker.Snapshots.Enqueue(new QueueDeclareOk("orders", 4, 1));
        broker.Snapshots.Enqueue(new QueueDeclareOk("orders", 0, 0));
        broker.Snapshots.Enqueue(new QueueDeclareOk("orders", 4, 0));
        var filter = new PurgeOnStartupFilter("orders");
        var next = new RecordingPipe();
        ChannelContext channel = broker.Channel(TestContext.Current.CancellationToken);

        await filter.SendAsync(channel, next);
        await filter.SendAsync(channel, next);
        Assert.Equal(0, broker.PurgeCalls);
        Assert.Equal(2, next.Calls);

        await filter.SendAsync(channel, next);

        Assert.Equal(1, broker.PurgeCalls);
        Assert.Equal(3, next.Calls);
        Assert.Equal("orders", broker.DeclaredQueue);
        Assert.Equal(TestContext.Current.CancellationToken, broker.DeclareToken);
    }

    private sealed class RecordingBroker
    {
        public readonly Queue<QueueDeclareOk> Snapshots = new();
        public readonly TaskCompletionSource PurgeEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<uint>? PurgeCompletion { get; set; }
        public Exception? FirstPurgeFailure { get; set; }
        public int PassiveDeclareCalls { get; private set; }
        public int PurgeCalls { get; private set; }
        public string? DeclaredQueue { get; private set; }
        public string? PurgedQueue { get; private set; }
        public CancellationToken DeclareToken { get; private set; }
        public CancellationToken PurgeToken { get; private set; }

        public ChannelContext Channel(CancellationToken token)
        {
            ChannelContext channel = DispatchProxy.Create<ChannelContext, ChannelProxy>();
            ((ChannelProxy)(object)channel).Handler = (method, args) => method.Name switch
            {
                "get_CancellationToken" => token,
                "QueueDeclarePassiveAsync" => DeclareAsync(args!),
                "QueuePurgeAsync" => PurgeAsync(args!),
                _ => throw new NotSupportedException($"Unexpected channel call: {method.Name}"),
            };
            return channel;
        }

        private Task<QueueDeclareOk> DeclareAsync(object?[] args)
        {
            PassiveDeclareCalls++;
            DeclaredQueue = Assert.IsType<string>(args[0]);
            DeclareToken = Assert.IsType<CancellationToken>(args[1]);
            return Task.FromResult(Snapshots.TryDequeue(out QueueDeclareOk? snapshot)
                ? snapshot
                : new QueueDeclareOk("orders", 3, 0));
        }

        private Task<uint> PurgeAsync(object?[] args)
        {
            PurgeCalls++;
            PurgedQueue = Assert.IsType<string>(args[0]);
            PurgeToken = Assert.IsType<CancellationToken>(args[1]);
            PurgeEntered.TrySetResult();
            if (PurgeCalls == 1 && FirstPurgeFailure is { } failure)
                return Task.FromException<uint>(failure);
            return PurgeCompletion?.Task ?? Task.FromResult(3U);
        }
    }

    private class ChannelProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[]?, object?> Handler { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            Handler(targetMethod ?? throw new InvalidOperationException("Missing proxy method."), args);
    }

    private sealed class RecordingPipe : IPipe<ChannelContext>
    {
        private int _calls;

        public int Calls => Volatile.Read(ref _calls);
        public ConcurrentQueue<ChannelContext> Contexts { get; } = new();

        public Task SendAsync(ChannelContext context)
        {
            Contexts.Enqueue(context);
            Interlocked.Increment(ref _calls);
            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context)
        {
        }
    }
}
