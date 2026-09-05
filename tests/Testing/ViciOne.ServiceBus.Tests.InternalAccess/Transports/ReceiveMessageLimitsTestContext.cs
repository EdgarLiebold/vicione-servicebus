using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Tests.InternalAccess.Transports;

/// <summary>
/// Minimal endpoint context used to exercise receive-body admission without creating a transport.
/// Every member outside the construction path fails loudly so a test cannot accidentally turn this
/// access bridge into a behavioral endpoint substitute.
/// </summary>
public sealed class ReceiveMessageLimitsTestContext : BasePipeContext, ReceiveEndpointContext
{
    public ReceiveMessageLimitsTestContext(MessageLimits limits, Uri inputAddress)
        : base(limits ?? throw new ArgumentNullException(nameof(limits)), TimeProvider.System)
    {
        InputAddress = inputAddress ?? throw new ArgumentNullException(nameof(inputAddress));
    }

    public TimeSpan? ConsumerStopTimeout => null;
    public TimeSpan? StopTimeout => null;
    public Uri InputAddress { get; }
    public bool IsBusEndpoint => false;
    public IReceiveEndpointObserver EndpointObservers => throw Unavailable();
    public IReceiveObserver ReceiveObservers => throw Unavailable();
    public IReceiveTransportObserver TransportObservers => throw Unavailable();
    public ILogContext LogContext => throw Unavailable();
    public IPublishTopology Publish => throw Unavailable();
    public IReceivePipe ReceivePipe => throw Unavailable();
    public IPublishEndpointProvider PublishEndpointProvider => throw Unavailable();
    public ISendEndpointProvider SendEndpointProvider => throw Unavailable();
    public IMessageRouteTable MessageRoutes => throw Unavailable();
    public Task DependenciesReady => throw Unavailable();
    public Task DependentsCompleted => throw Unavailable();
    public bool PublishFaults => false;
    public int PrefetchCount => 1;
    public int? ConcurrentMessageLimit => 1;
    public ISerialization Serialization => throw Unavailable();

    public Exception ConvertException(Exception exception, string message) => exception;

    public IReceivePipeDispatcher CreateReceivePipeDispatcher() => throw Unavailable();

    public ValueTask ResetAsync(CancellationToken cancellationToken = default) =>
        cancellationToken.IsCancellationRequested ? ValueTask.FromCanceled(cancellationToken) : ValueTask.CompletedTask;

    public void AddConsumeAgent(IAgent agent) => throw Unavailable();

    public void AddSendAgent(IAgent agent) => throw Unavailable();

    public ConnectHandle ConnectSendObserver(ISendObserver observer) => throw Unavailable();

    public ConnectHandle ConnectPublishObserver(IPublishObserver observer) => throw Unavailable();

    public ConnectHandle ConnectReceiveTransportObserver(IReceiveTransportObserver observer) => throw Unavailable();

    public ConnectHandle ConnectReceiveObserver(IReceiveObserver observer) => throw Unavailable();

    public ConnectHandle ConnectReceiveEndpointObserver(IReceiveEndpointObserver observer) => throw Unavailable();

    public void Probe(ProbeContext context)
    {
        throw Unavailable();
    }

    static NotSupportedException Unavailable() =>
        new("The receive-limit test context exposes only the construction path required by BaseReceiveContext.");
}
