using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.RabbitMq.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.RabbitMqTransport;

public sealed class RabbitMqChannelInitializationOwnershipTests
{
    static readonly TimeSpan WaitBound = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 0)]
    [InlineData(false, 1)]
    [InlineData(true, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 2)]
    [InlineData(false, 3)]
    [InlineData(true, 3)]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CONNECTION-LIFECYCLE", "acquired-channel-initialization-preserves-primary-and-retires-owned-channel")]
    public async Task Create_InitializationOwnsAcquiredChannelAndPreservesPrimaryAsync(bool wrapContext, int mode)
    {
        using var caller = new CancellationTokenSource();
        var channel = DispatchProxy.Create<IChannel, BrokerChannel>();
        var broker = (BrokerChannel)(object)channel;
        broker.Mode = mode;
        var connection = DispatchProxy.Create<IConnection, BrokerConnection>();
        var provider = (BrokerConnection)(object)connection;
        provider.Channel = channel;
        provider.Mode = mode;
        var logger = new CleanupLogger(mode == 2);
        var previous = LogContext.Current;
        RabbitMqConnectionContext? owner = null;
        ChannelContext? returnedContext = null;
        IChannel? returnedChannel = null;
        var agent = new Agent();
        Task? operation = null;
        Exception? primary = null;
        try
        {
            LogContext.ConfigureCurrentLogContext(logger);
            agent.SetReady();
            var topology = new RabbitMqTopologyConfiguration(RabbitMqBusFactory.CreateMessageTopology());
            var configuration = new RabbitMqBusConfiguration(topology);
            owner = new RabbitMqConnectionContext(connection, configuration.HostConfiguration,
                "public-channel-initialization", caller.Token);

            async Task CreateAsync()
            {
                if (wrapContext)
                    returnedContext = await owner.CreateChannelContextAsync(agent, 3, caller.Token);
                else
                    returnedChannel = await owner.CreateChannelAsync(3, caller.Token);
            }
            var invocationFailure = Record.Exception(() => { operation = CreateAsync(); });
            Assert.Null(invocationFailure);
            Assert.NotNull(operation);
            await provider.CreateEntered.Task.WaitAsync(WaitBound, CancellationToken.None);
            Assert.Equal(1, provider.CreateCalls);
            Assert.Equal(caller.Token, provider.CreateToken);
            Assert.False(provider.RawCreateTask.IsCompleted);
            Assert.False(operation.IsCompleted);
            Assert.Equal(0, broker.SetterCalls);
            provider.ReleaseCreate();

            if (mode is 1 or 2)
            {
                await Task.WhenAny(operation, broker.CloseEntered.Task).WaitAsync(WaitBound, CancellationToken.None);
                Assert.Equal(1, broker.SetterCalls);
                Assert.Equal(owner.ContinuationTimeout, broker.Timeout);
                if (operation.IsCompleted)
                {
                    var surfaced = await Record.ExceptionAsync(() => operation.WaitAsync(WaitBound, CancellationToken.None));
                    Assert.True(operation.IsFaulted);
                    Assert.Same(broker.SetterFailure, surfaced);
                }
                // First finite oracle: an acquired channel cannot be abandoned when its public SPI setter throws.
                Assert.Equal(1, broker.CloseCalls);
                Assert.False(operation.IsCompleted);
                Assert.False(broker.RawCloseTask.IsCompleted);
                Assert.Equal(0, broker.DisposeCalls);
                broker.ReleaseClose();
                await broker.DisposeEntered.Task.WaitAsync(WaitBound, CancellationToken.None);
                Assert.True(broker.RawCloseTask.IsCompleted);
                Assert.False(broker.RawDisposeTask.IsCompleted);
                Assert.False(operation.IsCompleted);
                broker.ReleaseDispose();
            }

            var observed = await Record.ExceptionAsync(() => operation.WaitAsync(WaitBound, CancellationToken.None));
            Assert.True(operation.IsCompleted);
            if (mode == 0)
            {
                Assert.Null(observed);
                Assert.True(operation.IsCompletedSuccessfully);
                Assert.Equal(1, broker.SetterCalls);
                Assert.Equal(owner.ContinuationTimeout, broker.Timeout);
                if (wrapContext)
                    Assert.Same(channel, returnedContext!.Channel);
                else
                    Assert.Same(channel, returnedChannel);
                Assert.Equal(0, broker.CloseCalls);
                Assert.Equal(0, broker.DisposeCalls);
            }
            else if (mode == 3)
            {
                Assert.Same(provider.CreateFailure, observed);
                Assert.True(operation.IsFaulted);
                Assert.Same(provider.CreateFailure, Assert.Single(operation.Exception!.InnerExceptions));
                Assert.Equal(0, broker.SetterCalls);
                Assert.Equal(0, broker.CloseCalls);
                Assert.Equal(0, broker.DisposeCalls);
            }
            else
            {
                Assert.Same(broker.SetterFailure, observed);
                Assert.True(operation.IsFaulted);
                Assert.Same(broker.SetterFailure, Assert.Single(operation.Exception!.InnerExceptions));
                Assert.Null(returnedChannel);
                Assert.Null(returnedContext);
                Assert.Equal(1, broker.DisposeCalls);
                Assert.Equal(new[] { "close", "dispose" }, broker.Trace);
                if (mode == 2)
                {
                    Assert.True(broker.RawCloseTask.IsFaulted);
                    Assert.Same(broker.CloseFailure, Assert.Single(broker.RawCloseTask.Exception!.InnerExceptions));
                    Assert.True(broker.RawDisposeTask.IsFaulted);
                    Assert.Same(broker.DisposeFailure, Assert.Single(broker.RawDisposeTask.Exception!.InnerExceptions));
                    Assert.Equal(new[] { broker.CloseFailure, broker.DisposeFailure }, logger.Entries.Select(x => x.Exception));
                    Assert.Equal(2, logger.ThrowCount);
                }
                else
                {
                    Assert.True(broker.RawCloseTask.IsCompletedSuccessfully);
                    Assert.True(broker.RawDisposeTask.IsCompletedSuccessfully);
                    Assert.Empty(logger.Entries);
                }
            }
        }
        catch (Exception exception)
        {
            primary = exception;
            throw;
        }
        finally
        {
            logger.DisableThrow();
            broker.DisableFaults();
            provider.DisableFaults();
            provider.ReleaseCreate();
            broker.ReleaseClose();
            broker.ReleaseDispose();
            var failures = new List<Exception>();
            try
            {
                await CaptureCleanupAsync(() => ObserveTerminalAsync(operation, broker, provider), failures);
                if (provider.CreateCalls != 0)
                    await CaptureCleanupAsync(() => ObserveTerminalAsync(provider.RawCreateTask, broker, provider), failures);
                // A returned context owns its CTS and channel. Otherwise the test retires an acquired channel
                // if the product failed to start cleanup; no successful broker release is promised for native faults.
                if (returnedContext != null)
                    await CaptureCleanupAsync(() => ObserveTerminalAsync(
                        ((IAsyncDisposable)returnedContext).DisposeAsync().AsTask(), broker, provider), failures);
                else if (provider.RawCreateTask.IsCompletedSuccessfully && broker.DisposeCalls == 0)
                    await CaptureCleanupAsync(() => ObserveTerminalAsync(channel.CleanupAsync(), broker, provider), failures);
                if (broker.CloseCalls != 0)
                    await CaptureCleanupAsync(() => ObserveTerminalAsync(broker.RawCloseTask, broker, provider), failures);
                if (broker.DisposeCalls != 0)
                    await CaptureCleanupAsync(() => ObserveTerminalAsync(broker.RawDisposeTask, broker, provider), failures);
                if (owner != null)
                    await CaptureCleanupAsync(() => ObserveTerminalAsync(owner.DisposeAsync().AsTask(), broker, provider), failures);
                await CaptureCleanupAsync(() => ObserveTerminalAsync(agent.StopAsync("test channel cleanup", CancellationToken.None), broker, provider), failures);
                await CaptureCleanupAsync(() => ObserveTerminalAsync(agent.Completed, broker, provider), failures);
                await CaptureCleanupAsync(() => ObserveTerminalAsync(agent.Ready, broker, provider), failures);
            }
            finally
            {
                LogContext.Current = previous;
            }
            if (failures.Count != 0)
            {
                if (primary != null)
                    failures.Insert(0, primary);
                if (failures.Count == 1)
                    ExceptionDispatchInfo.Capture(failures[0]).Throw();
                throw new AggregateException(failures);
            }
        }
    }

    static async Task CaptureCleanupAsync(Func<Task> action, List<Exception> failures)
    {
        try { await action(); }
        catch (Exception exception) { failures.Add(exception); }
    }

    static async Task ObserveTerminalAsync(Task? task, BrokerChannel broker, BrokerConnection provider)
    {
        if (task == null)
            return;
        try { await task.WaitAsync(WaitBound, CancellationToken.None); }
        catch (Exception exception) when (task.IsCompleted && exception is not TimeoutException
            && (ReferenceEquals(exception, broker.SetterFailure) || ReferenceEquals(exception, broker.CloseFailure)
                || ReferenceEquals(exception, broker.DisposeFailure) || ReferenceEquals(exception, provider.CreateFailure)))
        {
        }
    }

    public class BrokerConnection : DispatchProxy
    {
        readonly TaskCompletionSource<IChannel> _create = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int _createCalls;
        bool _faultsEnabled = true;
        public IChannel Channel { get; set; } = null!;
        public int Mode { get; set; }
        public IOException CreateFailure { get; } = new("unique provider channel creation failure");
        public TaskCompletionSource CreateEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<IChannel> RawCreateTask => _create.Task;
        public int CreateCalls => Volatile.Read(ref _createCalls);
        public CancellationToken CreateToken { get; private set; }
        public void DisableFaults() => _faultsEnabled = false;
        public void ReleaseCreate()
        {
            if (Mode == 3 && _faultsEnabled) _create.TrySetException(CreateFailure);
            else _create.TrySetResult(Channel);
        }
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method?.Name == nameof(IConnection.CreateChannelAsync) && args is { Length: 2 })
            {
                CreateToken = (CancellationToken)args[1]!;
                Interlocked.Increment(ref _createCalls);
                CreateEntered.TrySetResult();
                return RawCreateTask;
            }
            if (method?.Name == "get_IsOpen") return true;
            if (method?.Name == nameof(IConnection.CloseAsync) && args is { Length: 5 }) return Task.CompletedTask;
            if (method?.Name == nameof(IAsyncDisposable.DisposeAsync) && args is null or { Length: 0 }) return ValueTask.CompletedTask;
            throw new NotSupportedException(method?.ToString());
        }
    }

    public class BrokerChannel : DispatchProxy
    {
        readonly TaskCompletionSource _close = new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly TaskCompletionSource _dispose = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int _setterCalls;
        int _closeCalls;
        int _disposeCalls;
        bool _faultsEnabled = true;
        public int Mode { get; set; }
        public IOException SetterFailure { get; } = new("unique public SPI timeout setter failure");
        public IOException CloseFailure { get; } = new("unique acquired channel close failure");
        public IOException DisposeFailure { get; } = new("unique acquired channel disposal failure");
        public TaskCompletionSource CloseEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource DisposeEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task RawCloseTask => _close.Task;
        public Task RawDisposeTask => _dispose.Task;
        public int SetterCalls => Volatile.Read(ref _setterCalls);
        public int CloseCalls => Volatile.Read(ref _closeCalls);
        public int DisposeCalls => Volatile.Read(ref _disposeCalls);
        public TimeSpan Timeout { get; private set; }
        public List<string> Trace { get; } = new();
        public void DisableFaults() => _faultsEnabled = false;
        public void ReleaseClose()
        {
            if (Mode == 2 && _faultsEnabled) _close.TrySetException(CloseFailure);
            else _close.TrySetResult();
        }
        public void ReleaseDispose()
        {
            if (Mode == 2 && _faultsEnabled) _dispose.TrySetException(DisposeFailure);
            else _dispose.TrySetResult();
        }
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method?.Name == "set_ContinuationTimeout" && args is { Length: 1 })
            {
                Timeout = (TimeSpan)args[0]!;
                Interlocked.Increment(ref _setterCalls);
                if (Mode is 1 or 2 && _faultsEnabled) throw SetterFailure;
                return null;
            }
            if (method?.Name == "get_IsOpen") return true;
            if (method?.Name == nameof(IChannel.CloseAsync) && args is { Length: 4 })
            {
                Trace.Add("close");
                Interlocked.Increment(ref _closeCalls);
                CloseEntered.TrySetResult();
                return RawCloseTask;
            }
            if (method?.Name == nameof(IAsyncDisposable.DisposeAsync) && args is null or { Length: 0 })
            {
                Trace.Add("dispose");
                Interlocked.Increment(ref _disposeCalls);
                DisposeEntered.TrySetResult();
                return new ValueTask(RawDisposeTask);
            }
            throw new NotSupportedException(method?.ToString());
        }
    }

    sealed class CleanupLogger(bool hostile) : ILogger
    {
        int _throwEnabled = hostile ? 1 : 0;
        public List<CleanupEntry> Entries { get; } = new();
        public int ThrowCount { get; private set; }
        public void DisableThrow() => Interlocked.Exchange(ref _throwEnabled, 0);
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => level == LogLevel.Error;
        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (level != LogLevel.Error) return;
            var values = ((IEnumerable<KeyValuePair<string, object?>>)(object)state!).ToDictionary(x => x.Key, x => x.Value);
            var template = (string)values["{OriginalFormat}"]!;
            if (!template.StartsWith("Closing the channel faulted", StringComparison.Ordinal)
                && !template.StartsWith("Disposing the channel faulted", StringComparison.Ordinal)) return;
            Entries.Add(new CleanupEntry(template, exception));
            if (Volatile.Read(ref _throwEnabled) != 0)
            {
                ThrowCount++;
                throw new ApplicationException("unique optional acquired channel cleanup diagnostic failure");
            }
        }
    }
    sealed record CleanupEntry(string Template, Exception? Exception);
}
