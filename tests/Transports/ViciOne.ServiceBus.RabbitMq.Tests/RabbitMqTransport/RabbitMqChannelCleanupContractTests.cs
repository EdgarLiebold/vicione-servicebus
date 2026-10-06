using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.RabbitMqTransport;

public sealed class RabbitMqChannelCleanupContractTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CHANNEL-CLEANUP", "close-dispose-owned-despite-diagnostic-failure")]
    public async Task CleanupAsync_AttemptsAndJoinsCloseAndDisposeDespiteDiagnosticFailureAsync(int failureStage, bool hostileLogger)
    {
        using var caller = new CancellationTokenSource();
        var providerFailure = new IOException("unique Rabbit channel cleanup provider failure");
        var diagnosticFailure = new ApplicationException("unique Rabbit channel cleanup diagnostic failure");
        var channel = DispatchProxy.Create<IChannel, CleanupChannel>();
        var fixture = (CleanupChannel)(object)channel;
        var logger = new CleanupLogger(hostileLogger ? diagnosticFailure : null);
        var previous = LogContext.Current;
        Task? operation = null;
        Task? fallbackDisposal = null;
        try
        {
            LogContext.ConfigureCurrentLogContext(logger);
            operation = channel.CleanupAsync(321, "owned cleanup sentinel", caller.Token);
            Assert.Equal(1, fixture.CloseCalls);
            Assert.Equal((ushort)321, fixture.ReplyCode);
            Assert.Equal("owned cleanup sentinel", fixture.ReplyText);
            Assert.False(fixture.Abort);
            Assert.Equal(caller.Token, fixture.CloseToken);
            Assert.False(fixture.CloseTask.IsCompleted);
            Assert.False(operation.IsCompleted);
            Assert.Equal(0, fixture.DisposeCalls);

            fixture.ReleaseClose(failureStage == 1 ? providerFailure : null);
            await Task.WhenAny(operation, fixture.DisposeStarted.Task)
                .WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
            Assert.True(fixture.CloseTask.IsCompleted);
            if (failureStage == 1)
            {
                Assert.True(fixture.CloseTask.IsFaulted);
                var entry = Assert.Single(logger.Entries);
                Assert.Same(providerFailure, entry.Exception);
                Assert.Equal("Closing the channel faulted, the primary failure is unaffected: {ReplyCode} {Message}", entry.Template);
                Assert.Equal((ushort)321, entry.ReplyCode);
                Assert.Equal("owned cleanup sentinel", entry.Message);
                Assert.Equal(hostileLogger ? 1 : 0, logger.ThrowCount);
            }
            else
                Assert.Empty(logger.Entries);

            // The old close-failure logger branch ends the public operation before Dispose is admitted.
            Assert.Equal(1, fixture.DisposeCalls);
            Assert.Equal(new[] { "close", "dispose" }, fixture.Trace);
            Assert.False(fixture.DisposeTask.IsCompleted);
            Assert.False(operation.IsCompleted);
            fixture.ReleaseDispose(failureStage == 2 ? providerFailure : null);
            var observed = await Record.ExceptionAsync(async () =>
                await operation.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None));
            Assert.True(fixture.CloseTask.IsCompleted);
            Assert.True(fixture.DisposeTask.IsCompleted);
            Assert.True(operation.IsCompleted);
            if (failureStage == 2)
            {
                Assert.True(fixture.DisposeTask.IsFaulted);
                var entry = Assert.Single(logger.Entries);
                Assert.Same(providerFailure, entry.Exception);
                Assert.Equal("Disposing the channel faulted, the primary failure is unaffected: {ReplyCode} {Message}", entry.Template);
                Assert.Equal((ushort)321, entry.ReplyCode);
                Assert.Equal("owned cleanup sentinel", entry.Message);
                Assert.Equal(hostileLogger ? 1 : 0, logger.ThrowCount);
            }
            if (observed != null)
                Assert.Same(diagnosticFailure, observed);
            Assert.Null(observed);
            Assert.True(operation.IsCompletedSuccessfully);
            Assert.Equal(1, fixture.CloseCalls);
            Assert.Equal(1, fixture.DisposeCalls);
            if (failureStage == 0)
            {
                Assert.Empty(logger.Entries);
                Assert.Equal(0, logger.ThrowCount);
                Assert.True(fixture.CloseTask.IsCompletedSuccessfully);
                Assert.True(fixture.DisposeTask.IsCompletedSuccessfully);
            }
        }
        finally
        {
            fixture.ReleaseClose(null);
            fixture.ReleaseDispose(null);
            try
            {
                if (operation != null)
                    await ObserveTerminalAsync(operation);
            }
            finally
            {
                try
                {
                    await ObserveTerminalAsync(fixture.CloseTask);
                }
                finally
                {
                    try
                    {
                        // Decide skipped disposal only after the real helper is terminal.
                        if ((operation == null || operation.IsCompleted) && fixture.DisposeCalls == 0)
                            fallbackDisposal = channel.DisposeAsync().AsTask();
                        await ObserveTerminalAsync(fixture.DisposeTask);
                    }
                    finally
                    {
                        try
                        {
                            if (fallbackDisposal != null)
                                await ObserveTerminalAsync(fallbackDisposal);
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

    static async Task ObserveTerminalAsync(Task task)
    {
        try
        {
            await task.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
        }
        catch (Exception exception) when (task.IsCompleted && exception is not TimeoutException)
        {
        }
    }

    public class CleanupChannel : DispatchProxy
    {
        readonly TaskCompletionSource _close = new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly TaskCompletionSource _dispose = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource DisposeStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task CloseTask => _close.Task;
        public Task DisposeTask => _dispose.Task;
        public int CloseCalls { get; private set; }
        public int DisposeCalls { get; private set; }
        public ushort ReplyCode { get; private set; }
        public string? ReplyText { get; private set; }
        public bool Abort { get; private set; }
        public CancellationToken CloseToken { get; private set; }
        public List<string> Trace { get; } = new();

        public void ReleaseClose(Exception? failure)
        {
            if (failure == null)
                _close.TrySetResult();
            else
                _close.TrySetException(failure);
        }

        public void ReleaseDispose(Exception? failure)
        {
            if (failure == null)
                _dispose.TrySetResult();
            else
                _dispose.TrySetException(failure);
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == "get_IsOpen")
                return true;
            if (targetMethod?.Name == nameof(IChannel.CloseAsync) && args is { Length: 4 })
            {
                CloseCalls++;
                ReplyCode = (ushort)args[0]!;
                ReplyText = (string)args[1]!;
                Abort = (bool)args[2]!;
                CloseToken = (CancellationToken)args[3]!;
                Trace.Add("close");
                return CloseTask;
            }
            if (targetMethod?.Name == nameof(IAsyncDisposable.DisposeAsync))
            {
                DisposeCalls++;
                Trace.Add("dispose");
                DisposeStarted.TrySetResult();
                return new ValueTask(DisposeTask);
            }
            throw new NotSupportedException(targetMethod?.ToString());
        }
    }

    sealed class CleanupLogger(Exception? failure) : ILogger
    {
        public List<LogEntry> Entries { get; } = new();
        public int ThrowCount { get; private set; }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Error;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel != LogLevel.Error)
                return;
            var values = ((IEnumerable<KeyValuePair<string, object?>>)(object)state!).ToDictionary(x => x.Key, x => x.Value);
            Entries.Add(new LogEntry(exception, (string)values["{OriginalFormat}"]!,
                (ushort)values["ReplyCode"]!, (string)values["Message"]!));
            if (failure != null)
            {
                ThrowCount++;
                throw failure;
            }
        }
    }

    sealed record LogEntry(Exception? Exception, string Template, ushort ReplyCode, string Message);
}
