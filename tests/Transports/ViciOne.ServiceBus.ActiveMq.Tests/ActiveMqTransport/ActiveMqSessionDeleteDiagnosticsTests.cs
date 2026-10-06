using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Apache.NMS;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.ActiveMq.Configuration;
using ViciOne.ServiceBus.ActiveMq.Tests.TestDoubles;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMq.Tests.ActiveMqTransport;

public sealed class ActiveMqSessionDeleteDiagnosticsTests
{
    const string EntityName = "public-owned-diagnostic-delete";
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
    [InlineData(false, 4)]
    [InlineData(true, 4)]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-LIFECYCLE", "session-delete-own-debug-preserves-native-outcome")]
    public async Task SessionDelete_OwnDiagnosticPreservesNativeOutcomeAndCancellationAsync(bool topic, int mode)
    {
        using var caller = new CancellationTokenSource();
        var previous = LogContext.Current;
        var fixture = new Fixture(topic, mode == 2);
        var loggerFailure = new IOException("unique selected session-delete diagnostic failure");
        var logger = new SelectedLogger(topic ? "Delete topic: {Topic}" : "Delete queue: {Queue}", mode == 1, loggerFailure);
        ActiveMqConnectionContext? connection = null;
        ActiveMqSessionContext? context = null;
        Task<IMessageConsumer>? creation = null;
        Task? deletion = null;
        Exception? primary = null;
        try
        {
            LogContext.ConfigureCurrentLogContext(logger);
            var topology = new ActiveMqTopologyConfiguration(ActiveMqBusFactory.CreateMessageTopology());
            var configuration = new ActiveMqBusConfiguration(topology);
            configuration.HostConfiguration.Settings = new OpenWireHostSettings(new Uri("activemq://broker.internal:61616"));
            connection = new ActiveMqConnectionContext(fixture.NativeConnection, configuration.HostConfiguration, CancellationToken.None);
            context = new ActiveMqSessionContext(connection, fixture.NativeSession, CancellationToken.None);

            // This is the real asynchronous native consumer operation on the context's sole worker.
            creation = context.CreateMessageConsumerAsync(fixture.BarrierQueue, null, false, cancellationToken: CancellationToken.None);
            await fixture.ConsumerEntered.Task.WaitAsync(WaitBound, CancellationToken.None);
            Assert.False(fixture.RawConsumerTask.IsCompleted);
            Assert.False(creation.IsCompleted);
            Assert.Equal(1, fixture.ConsumerCalls);
            Assert.Same(fixture.BarrierQueue, fixture.ConsumerDestination);
            Assert.Null(fixture.ConsumerSelector);
            Assert.False(fixture.ConsumerNoLocal);
            if (mode == 3)
                caller.Cancel();

            // These methods are nonasync and may throw before returning any Task.
            Exception? invocationFailure = Record.Exception(() =>
            {
                deletion = topic
                    ? context.DeleteTopicAsync(EntityName, caller.Token)
                    : context.DeleteQueueAsync(EntityName, caller.Token);
            });
            Assert.Empty(fixture.Resolutions);
            Assert.Empty(fixture.Deletes);
            if (deletion is not null && mode != 3)
                Assert.False(deletion.IsCompleted);
            if (mode == 4)
                caller.Cancel();

            fixture.ReleaseConsumer();
            Assert.Same(fixture.Consumer, await creation.WaitAsync(WaitBound, CancellationToken.None));
            Assert.True(fixture.RawConsumerTask.IsCompletedSuccessfully);
            Exception? operationFailure = deletion is null ? null
                : await Record.ExceptionAsync(() => deletion.WaitAsync(WaitBound, CancellationToken.None));
            if (deletion is not null)
                Assert.True(deletion.IsCompleted);
            if (mode == 3)
            {
                Assert.Empty(logger.Emissions);
                Assert.Equal(0, logger.ThrowCount);
            }
            else
            {
                var emission = Assert.Single(logger.Emissions);
                Assert.Equal(logger.Template, emission["{OriginalFormat}"]);
                Assert.Equal(EntityName, emission[topic ? "Topic" : "Queue"]);
                Assert.Equal(mode == 1 ? 1 : 0, logger.ThrowCount);
                if (invocationFailure is not null)
                    Assert.Same(loggerFailure, invocationFailure);
            }

            // First finite diagnostic oracle: successful deletion must be admitted despite its own optional logger.
            Assert.Null(invocationFailure);
            Assert.NotNull(deletion);
            if (mode is 3 or 4)
            {
                var canceled = Assert.IsAssignableFrom<OperationCanceledException>(operationFailure);
                Assert.Equal(caller.Token, canceled.CancellationToken);
                Assert.True(deletion.IsCanceled);
                Assert.Empty(fixture.Resolutions);
                Assert.Empty(fixture.Deletes);
            }
            else
            {
                Assert.Equal(EntityName, Assert.Single(fixture.Resolutions));
                Assert.Same(fixture.Target, Assert.Single(fixture.Deletes));
                if (mode == 2)
                {
                    Assert.True(deletion.IsFaulted);
                    Assert.Same(fixture.NativeFailure, operationFailure);
                    Assert.Same(fixture.NativeFailure, Assert.Single(deletion.Exception!.InnerExceptions));
                }
                else
                {
                    Assert.Null(operationFailure);
                    Assert.True(deletion.IsCompletedSuccessfully);
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
            fixture.ReleaseConsumer();
            var failures = new List<Exception>();
            try
            {
                await CaptureCleanupAsync(() => ObserveTerminalAsync(fixture.RawConsumerTask), failures);
                await CaptureCleanupAsync(() => ObserveTerminalAsync(creation), failures);
                await CaptureCleanupAsync(() => ObserveTerminalAsync(deletion), failures);
                if (context is not null)
                    await CaptureCleanupAsync(() => context.DisposeAsync().AsTask().WaitAsync(WaitBound, CancellationToken.None), failures);
                await CaptureCleanupAsync(() =>
                {
                    fixture.Consumer.Dispose();
                    return Task.CompletedTask;
                }, failures);
                if (connection is not null)
                    await CaptureCleanupAsync(() => connection.DisposeAsync().AsTask().WaitAsync(WaitBound, CancellationToken.None), failures);
                if (failures.Count != 0)
                {
                    if (primary is not null)
                        failures.Insert(0, primary);
                    if (failures.Count == 1)
                        ExceptionDispatchInfo.Capture(failures[0]).Throw();
                    throw new AggregateException("Owned session-delete fixture cleanup did not finish.", failures);
                }
            }
            finally
            {
                LogContext.Current = previous;
            }
        }
    }

    static async Task CaptureCleanupAsync(Func<Task> action, List<Exception> failures)
    {
        try { await action(); }
        catch (Exception exception) { failures.Add(exception); }
    }

    static async Task ObserveTerminalAsync(Task? task)
    {
        if (task is null)
            return;
        try { await task.WaitAsync(WaitBound, CancellationToken.None); }
        catch (Exception exception) when (exception is not TimeoutException
            && ((task.IsCanceled && exception is OperationCanceledException)
                || (task.IsFaulted && task.Exception!.InnerExceptions.Any(cause => ReferenceEquals(cause, exception)))))
        {
        }
    }

    sealed class Fixture
    {
        readonly TaskCompletionSource<IMessageConsumer> _consumer = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int _consumerCalls;
        public TaskCompletionSource ConsumerEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<IMessageConsumer> RawConsumerTask => _consumer.Task;
        public IMessageConsumer Consumer { get; }
        public IQueue BarrierQueue { get; }
        public IDestination Target { get; }
        public IConnection NativeConnection { get; }
        public ISession NativeSession { get; }
        public NMSException NativeFailure { get; } = new("unique actual synchronous native DeleteDestination failure");
        public ConcurrentQueue<string> Resolutions { get; } = new();
        public ConcurrentQueue<IDestination> Deletes { get; } = new();
        public int ConsumerCalls => Volatile.Read(ref _consumerCalls);
        public IDestination? ConsumerDestination { get; private set; }
        public string? ConsumerSelector { get; private set; }
        public bool ConsumerNoLocal { get; private set; }

        public Fixture(bool topic, bool nativeFails)
        {
            BarrierQueue = InterfaceProxy<IQueue>.Create((method, _) => method.Name switch
            {
                "get_IsQueue" => true,
                "get_IsTopic" => false,
                "get_QueueName" => "owned-executor-barrier",
                _ => throw new NotSupportedException(method.ToString())
            });
            Target = topic
                ? InterfaceProxy<ITopic>.Create((method, _) => method.Name switch
                {
                    "get_IsQueue" => false,
                    "get_IsTopic" => true,
                    "get_TopicName" => EntityName,
                    _ => throw new NotSupportedException(method.ToString())
                })
                : InterfaceProxy<IQueue>.Create((method, _) => method.Name switch
                {
                    "get_IsQueue" => true,
                    "get_IsTopic" => false,
                    "get_QueueName" => EntityName,
                    _ => throw new NotSupportedException(method.ToString())
                });
            Consumer = InterfaceProxy<IMessageConsumer>.Create((method, _) => method.Name == nameof(IDisposable.Dispose)
                ? null : throw new NotSupportedException(method.ToString()));
            NativeConnection = InterfaceProxy<IConnection>.Create((method, _) => method.Name switch
            {
                nameof(IConnection.CloseAsync) => Task.CompletedTask,
                nameof(IDisposable.Dispose) => null,
                _ => throw new NotSupportedException(method.ToString())
            });
            NativeSession = InterfaceProxy<ISession>.Create((method, args) =>
            {
                if (method.Name == nameof(ISession.CreateConsumerAsync) && args is { Length: 3 })
                {
                    ConsumerDestination = (IDestination)args[0]!;
                    ConsumerSelector = (string?)args[1];
                    ConsumerNoLocal = (bool)args[2]!;
                    Interlocked.Increment(ref _consumerCalls);
                    ConsumerEntered.TrySetResult();
                    return RawConsumerTask;
                }
                if (method.Name == (topic ? nameof(ISession.GetTopic) : nameof(ISession.GetQueue)) && args is { Length: 1 })
                {
                    Resolutions.Enqueue((string)args[0]!);
                    return Target;
                }
                if (method.Name == nameof(ISession.DeleteDestination) && args is { Length: 1 })
                {
                    Deletes.Enqueue((IDestination)args[0]!);
                    if (nativeFails)
                        throw NativeFailure;
                    return null;
                }
                if (method.Name == nameof(ISession.CloseAsync))
                    return Task.CompletedTask;
                if (method.Name == nameof(IDisposable.Dispose))
                    return null;
                throw new NotSupportedException(method.ToString());
            });
        }

        public void ReleaseConsumer() => _consumer.TrySetResult(Consumer);
    }

    sealed class SelectedLogger(string template, bool hostile, Exception failure) : ILogger
    {
        int _throwEnabled = hostile ? 1 : 0;
        int _throwCount;
        public string Template => template;
        public ConcurrentQueue<Dictionary<string, object?>> Emissions { get; } = new();
        public int ThrowCount => Volatile.Read(ref _throwCount);
        public void DisableThrow() => Volatile.Write(ref _throwEnabled, 0);
        public bool IsEnabled(LogLevel level) => level == LogLevel.Debug;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (level != LogLevel.Debug || state is not IEnumerable<KeyValuePair<string, object?>> fields)
                return;
            var values = fields.ToDictionary(pair => pair.Key, pair => pair.Value);
            if (!values.TryGetValue("{OriginalFormat}", out var value) || !Equals(value, template))
                return;
            Emissions.Enqueue(values);
            if (Volatile.Read(ref _throwEnabled) == 0)
                return;
            Interlocked.Increment(ref _throwCount);
            throw failure;
        }
    }
}
