using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.EventHubs.LocalIntegration.Tests.EventHubIntegration;

public sealed class EventHubProducerOutcomeTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-EVENTHUB-PRODUCER-OUTCOME", "post-observer-failure-preserves-confirmed-single-and-batch-send")]
    public async Task PostObserverFailure_DoesNotRetryConfirmedMessagesOrReportSendFaultAsync(bool batch)
    {
        var context = new RecordingTransportContext();
        var observer = new FailingObserver(postFailureIndex: 1);
        await using var producer = new EventHubProducer(context);
        using ConnectHandle handle = producer.ConnectSendObserver(observer);
        Message[] messages = batch ? [new Message(1), new Message(2)] : [new Message(1)];

        if (batch)
            await producer.ProduceAsync<Message>(messages, TestContext.Current.CancellationToken);
        else
            await producer.ProduceAsync(messages[0], TestContext.Current.CancellationToken);

        Assert.Equal(messages.Select(message => message.Index), context.SubmittedIndices);
        Assert.Equal(1, context.ProviderCalls);
        Assert.Equal(messages.Select(message => message.Index), observer.PreIndices.Order());
        Assert.Equal(messages.Select(message => message.Index), observer.PostIndices.Order());
        Assert.Empty(observer.Faults);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-EVENTHUB-PRODUCER-OUTCOME", "fault-observer-cannot-mask-single-or-batch-provider-failure")]
    public async Task ProviderFailure_RemainsTheExactFailureWhenFaultObserverAlsoFailsAsync(bool batch)
    {
        var providerFailure = new ProviderFailureException();
        var context = new RecordingTransportContext(providerFailure);
        var observer = new FailingObserver(faultFailure: new ObserverFailureException());
        await using var producer = new EventHubProducer(context);
        using ConnectHandle handle = producer.ConnectSendObserver(observer);
        Message[] messages = batch ? [new Message(1), new Message(2)] : [new Message(1)];

        ProviderFailureException actual = await Assert.ThrowsAsync<ProviderFailureException>(() => batch
            ? producer.ProduceAsync<Message>(messages, TestContext.Current.CancellationToken)
            : producer.ProduceAsync(messages[0], TestContext.Current.CancellationToken));

        Assert.Same(providerFailure, actual);
        Assert.Equal(1, context.ProviderCalls);
        Assert.Equal(messages.Select(message => message.Index), context.SubmittedIndices);
        Assert.Equal(messages.Select(message => message.Index), observer.PreIndices.Order());
        Assert.Empty(observer.PostIndices);
        Assert.Equal(messages.Select(message => message.Index), observer.Faults.Select(fault => fault.Index).Order());
        Assert.All(observer.Faults, fault => Assert.Same(providerFailure, fault.Failure));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-EVENTHUB-PRODUCER-OUTCOME", "throwing-logger-preserves-single-and-batch-provider-outcome")]
    public async Task ThrowingLogger_CannotTurnConfirmedSendIntoFailureOrMaskProviderFailureAsync(bool batch, bool failProvider)
    {
        var providerFailure = new ProviderFailureException();
        var context = new RecordingTransportContext(failProvider ? providerFailure : null);
        var observer = new FailingObserver();
        await using var producer = new EventHubProducer(context);
        using ConnectHandle handle = producer.ConnectSendObserver(observer);
        Message[] messages = batch ? [new Message(1), new Message(2)] : [new Message(1)];
        ILogContext? previous = LogContext.Current;

        try
        {
            LogContext.ConfigureCurrentLogContext(new ThrowingLogger());
            Task send = batch
                ? producer.ProduceAsync<Message>(messages, TestContext.Current.CancellationToken)
                : producer.ProduceAsync(messages[0], TestContext.Current.CancellationToken);
            if (failProvider)
                Assert.Same(providerFailure, await Assert.ThrowsAsync<ProviderFailureException>(() => send));
            else
                await send;
        }
        finally
        {
            LogContext.Current = previous!;
        }

        Assert.Equal(1, context.ProviderCalls);
        Assert.Equal(messages.Select(message => message.Index), context.SubmittedIndices);
        Assert.Equal(failProvider ? Array.Empty<int>() : messages.Select(message => message.Index).ToArray(), observer.PostIndices.Order());
        Assert.Equal(failProvider ? messages.Length : 0, observer.Faults.Length);
    }

    private sealed record Message(int Index);
    private sealed class ProviderFailureException : Exception;
    private sealed class ObserverFailureException : Exception;

    private sealed class ThrowingLogger : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => throw new ObserverFailureException();
    }

    private sealed class FailingObserver(int? postFailureIndex = null, Exception? faultFailure = null) : ISendObserver
    {
        private readonly ConcurrentQueue<int> _pre = new();
        private readonly ConcurrentQueue<int> _post = new();
        private readonly ConcurrentQueue<(int Index, Exception Failure)> _faults = new();

        public int[] PreIndices => _pre.ToArray();
        public int[] PostIndices => _post.ToArray();
        public (int Index, Exception Failure)[] Faults => _faults.ToArray();

        public Task PreSendAsync<T>(SendContext<T> context) where T : class
        {
            _pre.Enqueue(((Message)(object)context.Message).Index);
            return Task.CompletedTask;
        }

        public Task PostSendAsync<T>(SendContext<T> context) where T : class
        {
            int index = ((Message)(object)context.Message).Index;
            _post.Enqueue(index);
            return index == postFailureIndex ? Task.FromException(new ObserverFailureException()) : Task.CompletedTask;
        }

        public Task SendFaultAsync<T>(SendContext<T> context, Exception exception) where T : class
        {
            _faults.Enqueue((((Message)(object)context.Message).Index, exception));
            return faultFailure is null ? Task.CompletedTask : Task.FromException(faultFailure);
        }
    }

    private sealed class RecordingTransportContext(Exception? providerFailure = null) : BasePipeContext, EventHubSendTransportContext
    {
        private readonly List<int> _submittedIndices = [];

        public int ProviderCalls { get; private set; }
        public int[] SubmittedIndices => _submittedIndices.ToArray();
        public ILogContext LogContext { get; } = new QuietLogContext();
        public string EntityName => "outcome-test";
        public string ActivityName => "eventhub outcome test";
        public string ActivityDestination => "outcome-test";
        public string ActivitySystem => "eventhubs";
        public SendObservable SendObservers { get; } = new();
        public ISerialization Serialization => throw new NotSupportedException();

        public IEnumerable<IAgent> GetAgentHandles() => [];
        public ConnectHandle ConnectSendObserver(ISendObserver observer) => SendObservers.Connect(observer);

        public Task<SendContext<T>> CreateSendContextAsync<T>(T message, IPipe<SendContext<T>> pipe,
            CancellationToken cancellationToken) where T : class =>
            throw new NotSupportedException();

        public async Task<EventHubSendContext<T>> CreateContextAsync<T>(T value, IPipe<EventHubSendContext<T>> pipe,
            IPipe<SendContext<T>>? initializerPipe = null, CancellationToken cancellationToken = default) where T : class
        {
            var context = new EventHubMessageSendContext<T>(value, cancellationToken)
            {
                DestinationAddress = new Uri("loopback://localhost/outcome-test"),
            };
            if (initializerPipe is not null)
                await initializerPipe.SendAsync(context);
            await pipe.SendAsync(context);
            return context;
        }

        public Task SendAsync<T>(ProducerContext producerContext, EventHubSendContext<T> sendContext,
            CancellationToken cancellationToken = default) where T : class => Submit([sendContext]);

        public Task SendAsync<T>(ProducerContext producerContext, EventHubSendContext<T>[] sendContexts,
            CancellationToken cancellationToken = default) where T : class => Submit(sendContexts);

        public Task SendAsync(IPipe<ProducerContext> pipe, CancellationToken cancellationToken) => pipe.SendAsync(null!);

        public void Probe(ProbeContext context) { }

        private Task Submit<T>(EventHubSendContext<T>[] contexts) where T : class
        {
            ProviderCalls++;
            _submittedIndices.AddRange(contexts.Select(context => ((Message)(object)context.Message).Index));
            return providerFailure is null ? Task.CompletedTask : Task.FromException(providerFailure);
        }
    }

    private sealed class QuietLogContext : ILogContext
    {
        public ILogger Logger => NullLogger.Instance;
        public ILogContext Messages => this;
        public EnabledLogger? Critical => null;
        public EnabledLogger? Debug => null;
        public EnabledLogger? Error => null;
        public EnabledLogger? Info => null;
        public EnabledLogger? Trace => null;
        public EnabledLogger? Warning => null;
        public ILogContext CreateLogContext(string categoryName) => this;
    }
}
