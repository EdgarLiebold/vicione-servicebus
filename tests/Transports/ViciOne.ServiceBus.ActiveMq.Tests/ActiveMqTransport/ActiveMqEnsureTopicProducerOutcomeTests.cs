using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Apache.NMS;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.ServiceBus.ActiveMq.Configuration;
using ViciOne.ServiceBus.ActiveMq.Tests.TestDoubles;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMq.Tests.ActiveMqTransport;

public sealed class ActiveMqEnsureTopicProducerOutcomeTests
{
    const string TopicName = "public-producer-outcome";
    static readonly TimeSpan WaitBound = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-LIFECYCLE", "ensure-topic-preserves-producer-close-dispose-outcomes")]
    public async Task EnsureTopic_PreservesProducerCleanupOutcomesAndCancellationAsync(int mode)
    {
        using var caller = new CancellationTokenSource();
        var previous = LogContext.Current;
        var fixture = new Fixture(mode);
        ActiveMqConnectionContext? connection = null;
        ActiveMqSessionContext? context = null;
        Task<IMessageConsumer>? creation = null;
        Task? operation = null;
        Exception? primary = null;
        try
        {
            LogContext.ConfigureCurrentLogContext(NullLogger.Instance);
            var topology = new ActiveMqTopologyConfiguration(ActiveMqBusFactory.CreateMessageTopology());
            var configuration = new ActiveMqBusConfiguration(topology);
            configuration.HostConfiguration.Settings = new OpenWireHostSettings(new Uri("activemq://broker.internal:61616"));
            connection = new ActiveMqConnectionContext(fixture.NativeConnection, configuration.HostConfiguration, CancellationToken.None);
            context = new ActiveMqSessionContext(connection, fixture.NativeSession, CancellationToken.None);

            creation = context.CreateMessageConsumerAsync(fixture.BarrierQueue, null, false, cancellationToken: CancellationToken.None);
            await fixture.ConsumerEntered.Task.WaitAsync(WaitBound, CancellationToken.None);
            Assert.Equal(1, fixture.ConsumerCalls);
            Assert.Same(fixture.BarrierQueue, fixture.ConsumerDestination);
            Assert.Null(fixture.ConsumerSelector);
            Assert.False(fixture.ConsumerNoLocal);
            Assert.False(fixture.RawConsumerTask.IsCompleted);
            Assert.False(creation.IsCompleted);
            if (mode == 4)
                caller.Cancel();

            Exception? invocationFailure = Record.Exception(() => { operation = context.EnsureTopicExistsAsync(fixture.Topic, caller.Token); });
            Assert.Null(invocationFailure);
            Assert.NotNull(operation);
            Assert.Empty(fixture.Resolutions);
            Assert.Equal(0, fixture.ProducerCalls);
            Assert.Empty(fixture.Stages);
            if (mode != 4)
                Assert.False(operation.IsCompleted);
            if (mode == 5)
                caller.Cancel();

            fixture.ReleaseConsumer();
            Assert.Same(fixture.Consumer, await creation.WaitAsync(WaitBound, CancellationToken.None));
            Assert.True(fixture.RawConsumerTask.IsCompletedSuccessfully);
            Exception? failure = await Record.ExceptionAsync(() => operation.WaitAsync(WaitBound, CancellationToken.None));
            Assert.True(operation.IsCompleted);
            if (mode is 4 or 5)
            {
                var canceled = Assert.IsAssignableFrom<OperationCanceledException>(failure);
                Assert.Equal(caller.Token, canceled.CancellationToken);
                Assert.True(operation.IsCanceled);
                Assert.Empty(fixture.Resolutions);
                Assert.Equal(0, fixture.ProducerCalls);
                Assert.Empty(fixture.Stages);
            }
            else
            {
                Assert.Equal(TopicName, Assert.Single(fixture.Resolutions));
                Assert.Equal(1, fixture.ProducerCalls);
                Assert.Same(fixture.Target, fixture.ProducerDestination);
                Assert.Equal(new[] { "close", "dispose" }, fixture.Stages.ToArray());
                Assert.Equal(1, fixture.CloseCalls);
                Assert.Equal(1, fixture.DisposeCalls);
                if (mode == 0)
                {
                    Assert.Null(failure);
                    Assert.True(operation.IsCompletedSuccessfully);
                }
                else
                {
                    Assert.True(operation.IsFaulted);
                    if (mode == 3)
                    {
                        // Both cleanup stages belong to this public operation; neither native cause may be lost.
                        var aggregate = Assert.IsType<AggregateException>(failure);
                        Assert.Collection(aggregate.InnerExceptions,
                            cause => Assert.Same(fixture.CloseFailure, cause),
                            cause => Assert.Same(fixture.DisposeFailure, cause));
                        Assert.Same(aggregate, Assert.Single(operation.Exception!.InnerExceptions));
                    }
                    else
                    {
                        Exception expected = mode == 1 ? fixture.CloseFailure : fixture.DisposeFailure;
                        Assert.Same(expected, failure);
                        Assert.Same(expected, Assert.Single(operation.Exception!.InnerExceptions));
                    }
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
            fixture.ReleaseConsumer();
            var failures = new List<Exception>();
            try
            {
                await CaptureCleanupAsync(() => ObserveTerminalAsync(fixture.RawConsumerTask), failures);
                await CaptureCleanupAsync(() => ObserveTerminalAsync(creation), failures);
                await CaptureCleanupAsync(() => ObserveTerminalAsync(operation), failures);
                if (operation?.IsCompleted == true && fixture.ProducerCalls != 0 && fixture.DisposeCalls == 0)
                {
                    fixture.DisableProducerFailures();
                    await CaptureCleanupAsync(() =>
                    {
                        fixture.Producer.Dispose();
                        return Task.CompletedTask;
                    }, failures);
                }
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
                    throw new AggregateException("Owned EnsureTopic fixture cleanup did not finish.", failures);
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
        int _producerFailuresEnabled = 1;
        int _consumerCalls;
        int _producerCalls;
        int _closeCalls;
        int _disposeCalls;
        public TaskCompletionSource ConsumerEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<IMessageConsumer> RawConsumerTask => _consumer.Task;
        public IMessageConsumer Consumer { get; }
        public IMessageProducer Producer { get; }
        public IQueue BarrierQueue { get; }
        public ITopic Target { get; }
        public ViciOne.ServiceBus.ActiveMq.Topology.Topic Topic { get; }
        public IConnection NativeConnection { get; }
        public ISession NativeSession { get; }
        public NMSException CloseFailure { get; } = new("unique native EnsureTopic producer Close failure");
        public InvalidOperationException DisposeFailure { get; } = new("unique native EnsureTopic producer Dispose failure");
        public ConcurrentQueue<string> Resolutions { get; } = new();
        public ConcurrentQueue<string> Stages { get; } = new();
        public int ConsumerCalls => Volatile.Read(ref _consumerCalls);
        public int ProducerCalls => Volatile.Read(ref _producerCalls);
        public int CloseCalls => Volatile.Read(ref _closeCalls);
        public int DisposeCalls => Volatile.Read(ref _disposeCalls);
        public IDestination? ConsumerDestination { get; private set; }
        public string? ConsumerSelector { get; private set; }
        public bool ConsumerNoLocal { get; private set; }
        public IDestination? ProducerDestination { get; private set; }

        public Fixture(int mode)
        {
            BarrierQueue = InterfaceProxy<IQueue>.Create((method, _) => method.Name switch
            {
                "get_IsQueue" => true,
                "get_IsTopic" => false,
                "get_QueueName" => "ensure-topic-executor-barrier",
                _ => throw new NotSupportedException(method.ToString())
            });
            Target = InterfaceProxy<ITopic>.Create((method, _) => method.Name switch
            {
                "get_IsQueue" => false,
                "get_IsTopic" => true,
                "get_TopicName" => TopicName,
                _ => throw new NotSupportedException(method.ToString())
            });
            Topic = InterfaceProxy<ViciOne.ServiceBus.ActiveMq.Topology.Topic>.Create((method, _) => method.Name == "get_EntityName"
                ? TopicName + "?fixture-option=true" : throw new NotSupportedException(method.ToString()));
            Consumer = InterfaceProxy<IMessageConsumer>.Create((method, _) => method.Name == nameof(IDisposable.Dispose)
                ? null : throw new NotSupportedException(method.ToString()));
            Producer = InterfaceProxy<IMessageProducer>.Create((method, _) =>
            {
                if (method.Name == nameof(IMessageProducer.Close))
                {
                    Stages.Enqueue("close");
                    Interlocked.Increment(ref _closeCalls);
                    if (Volatile.Read(ref _producerFailuresEnabled) != 0 && mode is 1 or 3)
                        throw CloseFailure;
                    return null;
                }
                if (method.Name == nameof(IDisposable.Dispose))
                {
                    Stages.Enqueue("dispose");
                    Interlocked.Increment(ref _disposeCalls);
                    if (Volatile.Read(ref _producerFailuresEnabled) != 0 && mode is 2 or 3)
                        throw DisposeFailure;
                    return null;
                }
                throw new NotSupportedException(method.ToString());
            });
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
                if (method.Name == nameof(ISession.GetTopic) && args is { Length: 1 })
                {
                    Resolutions.Enqueue((string)args[0]!);
                    return Target;
                }
                if (method.Name == nameof(ISession.CreateProducer) && args is { Length: 1 })
                {
                    ProducerDestination = (IDestination)args[0]!;
                    Interlocked.Increment(ref _producerCalls);
                    return Producer;
                }
                if (method.Name == nameof(ISession.CloseAsync))
                    return Task.CompletedTask;
                if (method.Name == nameof(IDisposable.Dispose))
                    return null;
                throw new NotSupportedException(method.ToString());
            });
        }

        public void ReleaseConsumer() => _consumer.TrySetResult(Consumer);
        public void DisableProducerFailures() => Volatile.Write(ref _producerFailuresEnabled, 0);
    }
}
