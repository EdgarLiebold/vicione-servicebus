using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transports;

public sealed class SendTransportTests
{
    private static readonly Uri DestinationAddress = new("loopback://localhost/orders");

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-TRANSPORT-PIPELINE", "configuration-observer-dispatch-order-and-terminal-lifecycle")]
    public async Task SendAsync_AppliesConfigurationObserversAndDispatchInOrderAndRejectsAfterDisposalAsync()
    {
        var trace = new List<string>();
        var context = new RecordingSendTransportContext(trace);
        var observer = new RecordingSendObserver(trace);
        var transport = new SendTransport<TestTransportContext>(context);
        using ConnectHandle observerHandle = transport.ConnectSendObserver(observer);
        using var callerCancellation = new CancellationTokenSource();

        await transport.SendAsync(
            new SampleMessage(),
            new CallbackPipe<SendContext<SampleMessage>>(_ => trace.Add("configure")),
            callerCancellation.Token);

        Assert.Equal(["create", "configure", "pre", "send", "post"], trace);
        Assert.Equal(callerCancellation.Token, context.ObservedCancellationToken);
        Assert.NotNull(context.ObservedSendContext);

        await transport.DisposeAsync();
        await Assert.ThrowsAsync<TransportUnavailableException>(() =>
            transport.SendAsync(new SampleMessage(), Pipe.Empty<SendContext<SampleMessage>>(), CancellationToken.None));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-TRANSPORT-FAULT", "fault-observer-cannot-replace-send-failure")]
    public async Task SendFailure_RemainsAuthoritativeWhenTheFaultObserverAlsoFailsAsync()
    {
        var sendFailure = new ExpectedSendException();
        var observerFailure = new ExpectedObserverException();
        var trace = new List<string>();
        var context = new RecordingSendTransportContext(trace, sendFailure);
        var transport = new SendTransport<TestTransportContext>(context);
        using ConnectHandle observerHandle = transport.ConnectSendObserver(new RecordingSendObserver(trace, observerFailure));

        ExpectedSendException actual = await Assert.ThrowsAsync<ExpectedSendException>(() =>
            transport.SendAsync(
                new SampleMessage(),
                Pipe.Empty<SendContext<SampleMessage>>(),
                TestContext.Current.CancellationToken));

        Assert.Same(sendFailure, actual);
        Assert.Equal(["create", "pre", "send", "fault"], trace);
        await transport.DisposeAsync();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-TRANSPORT-CONTEXT", "public-context-creation-contract")]
    public async Task CreateSendContextAsync_ValidatesInputsAndReturnsTheConfiguredContextAsync()
    {
        var trace = new List<string>();
        var context = new RecordingSendTransportContext(trace);
        var transport = new SendTransport<TestTransportContext>(context);
        using var callerCancellation = new CancellationTokenSource();

        Assert.Equal(
            "message",
            (await Assert.ThrowsAsync<ArgumentNullException>(() =>
                transport.CreateSendContextAsync<SampleMessage>(null!, Pipe.Empty<SendContext<SampleMessage>>(), CancellationToken.None))).ParamName);
        Assert.Equal(
            "pipe",
            (await Assert.ThrowsAsync<ArgumentNullException>(() =>
                transport.CreateSendContextAsync(new SampleMessage(), null!, CancellationToken.None))).ParamName);

        SendContext<SampleMessage> sendContext = await transport.CreateSendContextAsync(
            new SampleMessage(),
            new CallbackPipe<SendContext<SampleMessage>>(_ => trace.Add("configure")),
            callerCancellation.Token);

        Assert.Equal(["create", "configure"], trace);
        Assert.Equal(DestinationAddress, sendContext.DestinationAddress);
        Assert.Equal(callerCancellation.Token, sendContext.CancellationToken);
        await transport.DisposeAsync();
    }

    private sealed class RecordingSendTransportContext(
        List<string> trace,
        Exception? sendFailure = null) : BasePipeContext, SendTransportContext<TestTransportContext>
    {
        private readonly List<string> _trace = trace ?? throw new ArgumentNullException(nameof(trace));

        public ILogContext LogContext { get; } = new BusLogContext(NullLoggerFactory.Instance);
        public string EntityName => "orders";
        public string ActivityName => "orders send";
        public string ActivityDestination => "orders";
        public string ActivitySystem => "test";
        public SendObservable SendObservers { get; } = new();
        public ISerialization Serialization => throw new NotSupportedException();
        public CancellationToken ObservedCancellationToken { get; private set; }
        public SendContext? ObservedSendContext { get; private set; }

        public IEnumerable<IAgent> GetAgentHandles() => [];

        public Task<SendContext<T>> CreateSendContextAsync<T>(
            T message,
            IPipe<SendContext<T>> pipe,
            CancellationToken cancellationToken)
            where T : class => CreateContextAsync(message, pipe, cancellationToken);

        public Task<SendContext<T>> CreateSendContextAsync<T>(
            TestTransportContext context,
            T message,
            IPipe<SendContext<T>> pipe,
            CancellationToken cancellationToken)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(context);
            return CreateContextAsync(message, pipe, cancellationToken);
        }

        public Task SendAsync<T>(
            TestTransportContext transportContext,
            SendContext<T> sendContext,
            CancellationToken cancellationToken = default)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(transportContext);
            ArgumentNullException.ThrowIfNull(sendContext);
            cancellationToken.ThrowIfCancellationRequested();
            _trace.Add("send");
            ObservedSendContext = sendContext;
            return sendFailure is null ? Task.CompletedTask : Task.FromException(sendFailure);
        }

        public Task SendAsync(IPipe<TestTransportContext> pipe, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(pipe);
            cancellationToken.ThrowIfCancellationRequested();
            return pipe.SendAsync(new TestTransportContext(cancellationToken));
        }

        public ConnectHandle ConnectSendObserver(ISendObserver observer) => SendObservers.Connect(observer);

        public void Probe(ProbeContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
        }

        private async Task<SendContext<T>> CreateContextAsync<T>(
            T message,
            IPipe<SendContext<T>> pipe,
            CancellationToken cancellationToken)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(message);
            ArgumentNullException.ThrowIfNull(pipe);
            cancellationToken.ThrowIfCancellationRequested();
            _trace.Add("create");
            ObservedCancellationToken = cancellationToken;
            var context = new MessageSendContext<T>(message, cancellationToken)
            {
                DestinationAddress = DestinationAddress,
            };
            await pipe.SendAsync(context);
            return context;
        }
    }

    private sealed class RecordingSendObserver(List<string> trace, Exception? faultFailure = null) : ISendObserver
    {
        public Task PreSendAsync<T>(SendContext<T> context)
            where T : class
        {
            trace.Add("pre");
            return Task.CompletedTask;
        }

        public Task PostSendAsync<T>(SendContext<T> context)
            where T : class
        {
            trace.Add("post");
            return Task.CompletedTask;
        }

        public Task SendFaultAsync<T>(SendContext<T> context, Exception exception)
            where T : class
        {
            trace.Add("fault");
            return faultFailure is null ? Task.CompletedTask : Task.FromException(faultFailure);
        }
    }

    private sealed class CallbackPipe<TContext>(Action<TContext> callback) : IPipe<TContext>
        where TContext : class, PipeContext
    {
        public Task SendAsync(TContext context)
        {
            callback(context);
            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
        }
    }

    private sealed class TestTransportContext(CancellationToken cancellationToken) : BasePipeContext(cancellationToken);
    private sealed class SampleMessage;
    private sealed class ExpectedSendException : Exception;
    private sealed class ExpectedObserverException : Exception;
}
