using Microsoft.Extensions.Logging;
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
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-SQL-STARTUP-PURGE", "owning-debug-preserves-once-only-purge-and-pipeline-progress")]
    public async Task SuccessfulPurge_RemainsOnceOnlyDespiteItsDebugLoggerAsync(bool selectSkip, bool loggerThrows)
    {
        using var caller = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        ClientContext client = DispatchProxy.Create<ClientContext, DiagnosticPurgeProxy>();
        var provider = (DiagnosticPurgeProxy)(object)client;
        provider.ContextToken = caller.Token;
        var next = new RecordingPipe();
        IFilter<ClientContext> filter = new PurgeOnStartupFilter("diagnostic-orders");
        var loggerFailure = new IOException("SQL startup purge diagnostic failure");
        string template = selectSkip
            ? "Queue {QueueName} was purged at startup, skipping"
            : "Purged queue {QueueName}";
        var logger = new PurgeDiagnosticLogger(template, loggerThrows ? loggerFailure : null);
        var previous = LogContext.Current;
        Task? first = null;
        Task? second = null;
        try
        {
            LogContext.ConfigureCurrentLogContext(logger);
            first = filter.SendAsync(client, next);
            await provider.Entered.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            Assert.Equal(1, provider.Calls);
            Assert.Equal("diagnostic-orders", provider.QueueName);
            Assert.Equal(caller.Token, provider.Token);
            Assert.False(provider.RawTask.IsCompleted);
            Assert.False(first.IsCompleted);
            Assert.Equal(0, next.CallCount);
            provider.Release();
            Exception? firstFailure = await Record.ExceptionAsync(() => first.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None));
            Assert.True(provider.RawTask.IsCompletedSuccessfully);
            if (firstFailure != null)
                Assert.Same(loggerFailure, firstFailure);
            second = filter.SendAsync(client, next);
            Exception? secondFailure = await Record.ExceptionAsync(() => second.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None));
            if (secondFailure != null)
                Assert.Same(loggerFailure, secondFailure);
            Assert.True(first.IsCompleted);
            Assert.True(second.IsCompleted);
            Assert.NotEmpty(logger.Entries);
            Assert.All(logger.Entries, entry =>
            {
                Assert.Equal(template, entry.Template);
                Assert.Equal("diagnostic-orders", entry.QueueName);
            });
            Assert.Equal(loggerThrows ? 1 : 0, logger.ThrowCount);
            // A successful database purge must stay recorded even when its optional completion log fails.
            Assert.Equal(1, provider.Calls);
            Assert.Null(firstFailure);
            Assert.Null(secondFailure);
            Assert.Equal(2, next.CallCount);
            Assert.True(first.IsCompletedSuccessfully);
            Assert.True(second.IsCompletedSuccessfully);
            Assert.Single(logger.Entries);
        }
        finally
        {
            provider.Release();
            try
            {
                if (first != null)
                    await ObservePurgeDiagnosticTaskAsync(first);
            }
            finally
            {
                try
                {
                    if (second != null)
                        await ObservePurgeDiagnosticTaskAsync(second);
                }
                finally
                {
                    try
                    {
                        if (provider.Calls > 0)
                            await ObservePurgeDiagnosticTaskAsync(provider.RawTask);
                    }
                    finally
                    {
                        LogContext.Current = previous;
                    }
                }
            }
        }
    }

    private static async Task ObservePurgeDiagnosticTaskAsync(Task task)
    {
        try
        {
            await task.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
        }
        catch (Exception exception) when (exception is not TimeoutException && (task.IsFaulted || task.IsCanceled))
        {
        }
    }

    private class DiagnosticPurgeProxy : DispatchProxy
    {
        readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly TaskCompletionSource<long> _raw = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task Entered => _entered.Task;
        public Task<long> RawTask => _raw.Task;
        public CancellationToken ContextToken { get; set; }
        public int Calls { get; private set; }
        public string? QueueName { get; private set; }
        public CancellationToken Token { get; private set; }
        public void Release() => _raw.TrySetResult(17L);
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method?.Name == "get_CancellationToken")
                return ContextToken;
            if (method?.Name != "PurgeQueueAsync")
                throw new NotSupportedException(method?.Name);
            Calls++;
            QueueName = (string)args![0]!;
            Token = (CancellationToken)args[1]!;
            _entered.TrySetResult();
            return _raw.Task;
        }
    }

    private sealed class PurgeDiagnosticLogger(string selectedTemplate, Exception? failure) : ILogger
    {
        public List<(string Template, string QueueName)> Entries { get; } = new();
        public int ThrowCount { get; private set; }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => level == LogLevel.Debug;
        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (level != LogLevel.Debug)
                return;
            var values = ((IEnumerable<KeyValuePair<string, object?>>)(object)state!).ToDictionary(x => x.Key, x => x.Value);
            if (!Equals(values["{OriginalFormat}"], selectedTemplate))
                return;
            Entries.Add((selectedTemplate, (string)values["QueueName"]!));
            if (failure != null && ThrowCount == 0)
            {
                ThrowCount++;
                throw failure;
            }
        }
    }

}
