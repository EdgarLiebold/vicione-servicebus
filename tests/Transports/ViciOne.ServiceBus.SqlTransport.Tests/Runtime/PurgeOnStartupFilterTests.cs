using System.Reflection;
using ViciOne.ServiceBus.SqlTransport;
using ViciOne.ServiceBus.SqlTransport.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.Tests.Runtime;

public sealed class PurgeOnStartupFilterTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-STARTUP-PURGE", "concurrent-pipelines-purge-exactly-once")]
    public async Task ConcurrentPipelines_PurgeTheQueueExactlyOnceAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var client = Client();
        var proxy = (PurgeClientContextProxy)(object)client;
        var next = new RecordingPipe();
        IFilter<ClientContext> filter = new PurgeOnStartupFilter("orders");

        Task first = filter.SendAsync(client, next);
        await proxy.PurgeEntered.WaitAsync(cancellationToken);

        Task second = filter.SendAsync(client, next);
        await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);

        Assert.Equal(1, proxy.PurgeCallCount);
        Assert.False(second.IsCompleted);

        proxy.ReleasePurge();
        await Task.WhenAll(first, second);

        Assert.Equal(1, proxy.PurgeCallCount);
        Assert.Equal(2, next.CallCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-STARTUP-PURGE", "failed-purge-remains-retryable")]
    public async Task FailedPurge_IsRetriedByTheNextPipelineAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var client = Client(failFirstPurge: true);
        var proxy = (PurgeClientContextProxy)(object)client;
        var next = new RecordingPipe();
        IFilter<ClientContext> filter = new PurgeOnStartupFilter("orders");

        await Assert.ThrowsAsync<InvalidOperationException>(() => filter.SendAsync(client, next));
        await filter.SendAsync(client, next).WaitAsync(cancellationToken);

        Assert.Equal(2, proxy.PurgeCallCount);
        Assert.Equal(1, next.CallCount);
    }

    private static ClientContext Client(bool failFirstPurge = false)
    {
        ClientContext client = DispatchProxy.Create<ClientContext, PurgeClientContextProxy>();
        ((PurgeClientContextProxy)(object)client).FailFirstPurge = failFirstPurge;
        return client;
    }

    private class PurgeClientContextProxy : DispatchProxy
    {
        readonly TaskCompletionSource<long> _purgeCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly TaskCompletionSource _purgeEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool FailFirstPurge { get; set; }
        public int PurgeCallCount { get; private set; }
        public Task PurgeEntered => _purgeEntered.Task;

        public void ReleasePurge() => _purgeCompletion.TrySetResult(1);

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            return targetMethod.Name switch
            {
                "get_CancellationToken" => CancellationToken.None,
                "PurgeQueueAsync" => PurgeQueueAsync(),
                _ => throw new NotSupportedException(targetMethod.Name),
            };
        }

        private Task<long> PurgeQueueAsync()
        {
            PurgeCallCount++;
            _purgeEntered.TrySetResult();

            if (FailFirstPurge && PurgeCallCount == 1)
                return Task.FromException<long>(new InvalidOperationException("purge failed"));

            return FailFirstPurge ? Task.FromResult(1L) : _purgeCompletion.Task;
        }
    }

    private sealed class RecordingPipe : IPipe<ClientContext>
    {
        private int _callCount;

        public int CallCount => Volatile.Read(ref _callCount);

        public Task SendAsync(ClientContext context)
        {
            Interlocked.Increment(ref _callCount);
            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context)
        {
        }
    }
}
