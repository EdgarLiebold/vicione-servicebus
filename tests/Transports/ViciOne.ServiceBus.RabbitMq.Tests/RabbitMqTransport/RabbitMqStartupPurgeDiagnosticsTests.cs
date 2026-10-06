using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using ViciOne.ServiceBus.RabbitMq.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.RabbitMqTransport;

public sealed class RabbitMqStartupPurgeDiagnosticsTests
{
    const string QueueName = "public-startup-purge-diagnostics";
    const string PurgedTemplate = "Purged {MessageCount} messages from queue {QueueName}";
    const string SkippedTemplate = "Startup purge decision for queue {QueueName} is complete, skipping";
    static readonly TimeSpan CleanupWait = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-RABBITMQ-STARTUP-PURGE", "successful-sdk-purge-and-skip-continue-despite-own-debug")]
    public async Task StartupPurge_ContinuesAfterSdkSuccessAndSkipsRepeatedPurgeDespiteDebugAsync(bool hostileLogger)
    {
        using var caller = new CancellationTokenSource();
        using var connectionLifetime = new CancellationTokenSource();
        var channel = DispatchProxy.Create<IChannel, BrokerChannel>();
        var broker = (BrokerChannel)(object)channel;
        var connection = InterfaceDouble.Create<ConnectionContext>((method, _) =>
            method.Name == "get_CancellationToken" ? connectionLifetime.Token : throw new NotSupportedException(method.ToString()));
        var agent = InterfaceDouble.Create<IAgent>((method, _) => throw new NotSupportedException(method.ToString()));
        var next = new RecordingPipe();
        var loggerFailure = new ApplicationException("unique public Rabbit startup purge diagnostic failure");
        var logger = new PurgeLogger(hostileLogger ? loggerFailure : null);
        var previous = LogContext.Current;
        RabbitMqChannelContext? context = null;
        Task? operation = null;
        Task? repeatedOperation = null;
        Task? disposal = null;
        try
        {
            LogContext.ConfigureCurrentLogContext(logger);
            context = new RabbitMqChannelContext(connection, channel, agent, caller.Token);
            IFilter<ChannelContext> filter = new PurgeOnStartupFilter(QueueName);
            var effectiveToken = context.CancellationToken;
            operation = filter.SendAsync(context, next);
            await broker.PurgeStarted.Task.WaitAsync(CleanupWait, CancellationToken.None);
            Assert.Equal((QueueName, effectiveToken), Assert.Single(broker.Declarations));
            Assert.Equal((QueueName, effectiveToken), Assert.Single(broker.Purges));
            Assert.False(effectiveToken.IsCancellationRequested);
            Assert.False(broker.PurgeTask.IsCompleted);
            Assert.False(operation.IsCompleted);
            Assert.Empty(next.Contexts);
            Assert.Empty(logger.Entries);

            broker.ReleasePurge();
            var observed = await Record.ExceptionAsync(async () =>
                await operation.WaitAsync(CleanupWait, CancellationToken.None));
            Assert.True(broker.PurgeTask.IsCompletedSuccessfully);
            Assert.Equal((uint)17, await broker.PurgeTask);
            Assert.True(operation.IsCompleted);
            var purged = Assert.Single(logger.Entries);
            Assert.Equal(PurgedTemplate, purged.Template);
            Assert.Equal(QueueName, purged.Queue);
            Assert.Equal((uint)17, purged.MessageCount);
            Assert.Null(purged.Exception);
            Assert.Equal(hostileLogger ? 1 : 0, logger.ThrowCount);
            if (observed != null)
                Assert.Same(loggerFailure, observed);
            Assert.Null(observed);
            Assert.True(operation.IsCompletedSuccessfully);
            Assert.Same(context, Assert.Single(next.Contexts));

            // Reinspection remains nonempty: the filter's successful first decision prevents another SDK purge.
            repeatedOperation = filter.SendAsync(context, next);
            var repeatedFailure = await Record.ExceptionAsync(async () =>
                await repeatedOperation.WaitAsync(CleanupWait, CancellationToken.None));
            Assert.True(repeatedOperation.IsCompleted);
            Assert.Equal(2, broker.Declarations.Count);
            Assert.Equal((QueueName, effectiveToken), broker.Declarations[1]);
            Assert.Single(broker.Purges);
            Assert.Equal(2, logger.Entries.Count);
            var skipped = logger.Entries[1];
            Assert.Equal(SkippedTemplate, skipped.Template);
            Assert.Equal(QueueName, skipped.Queue);
            Assert.Null(skipped.MessageCount);
            Assert.Null(skipped.Exception);
            Assert.Equal(hostileLogger ? 2 : 0, logger.ThrowCount);
            if (repeatedFailure != null)
                Assert.Same(loggerFailure, repeatedFailure);
            Assert.Null(repeatedFailure);
            Assert.True(repeatedOperation.IsCompletedSuccessfully);
            Assert.Equal(2, next.Contexts.Count);
            Assert.Same(context, next.Contexts[1]);

            disposal = context.DisposeAsync().AsTask();
            await disposal.WaitAsync(CleanupWait, CancellationToken.None);
            Assert.True(disposal.IsCompletedSuccessfully);
            Assert.True(broker.CloseTask.IsCompletedSuccessfully);
            Assert.True(broker.DisposeTask.IsCompletedSuccessfully);
            Assert.Equal(1, broker.CloseCalls);
            Assert.Equal(1, broker.DisposeCalls);
            Assert.Equal((ushort)200, broker.CloseCode);
            Assert.Equal("ChannelContext Disposed", broker.CloseText);
            Assert.False(broker.CloseAbort);
            Assert.Equal(CancellationToken.None, broker.CloseToken);
            Assert.Equal(new[] { "close", "dispose" }, broker.CleanupTrace);
        }
        finally
        {
            broker.ReleasePurge();
            try
            {
                if (operation != null)
                    await ObserveTerminalAsync(operation);
            }
            finally
            {
                try
                {
                    if (repeatedOperation != null)
                        await ObserveTerminalAsync(repeatedOperation);
                }
                finally
                {
                    try
                    {
                        await ObserveTerminalAsync(broker.PurgeTask);
                    }
                    finally
                    {
                        try
                        {
                            if (context != null)
                            {
                                disposal ??= context.DisposeAsync().AsTask();
                                await ObserveTerminalAsync(disposal);
                            }
                        }
                        finally
                        {
                            try
                            {
                                await ObserveTerminalAsync(broker.CloseTask);
                            }
                            finally
                            {
                                try
                                {
                                    await ObserveTerminalAsync(broker.DisposeTask);
                                }
                                finally
                                {
                                    LogContext.Current = previous;
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    static async Task ObserveTerminalAsync(Task task)
    {
        try
        {
            await task.WaitAsync(CleanupWait, CancellationToken.None);
        }
        catch (Exception exception) when (task.IsCompleted && exception is not TimeoutException)
        {
        }
    }

    public class BrokerChannel : DispatchProxy
    {
        readonly TaskCompletionSource<uint> _purge = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource PurgeStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<uint> PurgeTask => _purge.Task;
        public Task CloseTask { get; } = Task.CompletedTask;
        public Task DisposeTask { get; } = Task.CompletedTask;
        public List<(string Queue, CancellationToken Token)> Declarations { get; } = new();
        public List<(string Queue, CancellationToken Token)> Purges { get; } = new();
        public List<string> CleanupTrace { get; } = new();
        public int CloseCalls { get; private set; }
        public int DisposeCalls { get; private set; }
        public ushort CloseCode { get; private set; }
        public string? CloseText { get; private set; }
        public bool CloseAbort { get; private set; }
        public CancellationToken CloseToken { get; private set; }
        public void ReleasePurge() => _purge.TrySetResult(17);

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(IChannel.QueueDeclarePassiveAsync) && args is { Length: 2 })
            {
                Declarations.Add(((string)args[0]!, (CancellationToken)args[1]!));
                return Task.FromResult(new QueueDeclareOk(QueueName, 17, 0));
            }
            if (targetMethod?.Name == nameof(IChannel.QueuePurgeAsync) && args is { Length: 2 })
            {
                Purges.Add(((string)args[0]!, (CancellationToken)args[1]!));
                PurgeStarted.TrySetResult();
                return PurgeTask;
            }
            if (targetMethod?.Name == "get_IsOpen")
                return true;
            if (targetMethod?.Name == nameof(IChannel.CloseAsync) && args is { Length: 4 })
            {
                CloseCalls++;
                CloseCode = (ushort)args[0]!;
                CloseText = (string)args[1]!;
                CloseAbort = (bool)args[2]!;
                CloseToken = (CancellationToken)args[3]!;
                CleanupTrace.Add("close");
                return CloseTask;
            }
            if (targetMethod?.Name == nameof(IAsyncDisposable.DisposeAsync))
            {
                DisposeCalls++;
                CleanupTrace.Add("dispose");
                return new ValueTask(DisposeTask);
            }
            throw new NotSupportedException(targetMethod?.ToString());
        }
    }

    public class InterfaceDouble : DispatchProxy
    {
        Func<MethodInfo, object?[]?, object?> _invoke = null!;
        public static T Create<T>(Func<MethodInfo, object?[]?, object?> invoke) where T : class
        {
            var value = DispatchProxy.Create<T, InterfaceDouble>();
            ((InterfaceDouble)(object)value)._invoke = invoke;
            return value;
        }
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => _invoke(targetMethod!, args);
    }

    sealed class RecordingPipe : IPipe<ChannelContext>
    {
        public List<ChannelContext> Contexts { get; } = new();
        public void Probe(ProbeContext context) { }
        public Task SendAsync(ChannelContext context)
        {
            Contexts.Add(context);
            return Task.CompletedTask;
        }
    }

    sealed class PurgeLogger(Exception? failure) : ILogger
    {
        public List<LogEntry> Entries { get; } = new();
        public int ThrowCount { get; private set; }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Debug;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel != LogLevel.Debug)
                return;
            var values = ((IEnumerable<KeyValuePair<string, object?>>)(object)state!).ToDictionary(x => x.Key, x => x.Value);
            var template = (string)values["{OriginalFormat}"]!;
            if (template != PurgedTemplate && template != SkippedTemplate)
                return;
            Entries.Add(new LogEntry(template, (string)values["QueueName"]!,
                values.TryGetValue("MessageCount", out var count) ? (uint?)count : null, exception));
            if (failure != null)
            {
                ThrowCount++;
                throw failure;
            }
        }
    }

    sealed record LogEntry(string Template, string Queue, uint? MessageCount, Exception? Exception);
}
