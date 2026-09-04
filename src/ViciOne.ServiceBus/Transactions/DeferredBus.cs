using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Transactions;

internal abstract class DeferredBus :
    IBus
{
    readonly IBus _bus;
    readonly IPublishEndpoint _publishEndpoint;
    readonly DeferredBusPublishEndpointProvider _publishEndpointProvider;

    protected DeferredBus(IBus bus)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));

        _publishEndpointProvider = new DeferredBusPublishEndpointProvider(this, bus);
        _publishEndpoint = new PublishEndpoint(bus);
    }

    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        return _bus.ConnectPublishObserver(observer);
    }

    public Task<ISendEndpoint> GetPublishSendEndpoint<T>()
        where T : class
    {
        return _publishEndpointProvider.GetPublishSendEndpoint<T>();
    }

    public Task Publish<T>(T message, CancellationToken cancellationToken = default)
        where T : class
    {
        return Add(token => _publishEndpoint.Publish(message, token), cancellationToken);
    }

    public Task Publish<T>(T message, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default)
        where T : class
    {
        return Add(token => _publishEndpoint.Publish(message, publishPipe, token), cancellationToken);
    }

    public Task Publish<T>(T message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
        where T : class
    {
        return Add(token => _publishEndpoint.Publish(message, publishPipe, token), cancellationToken);
    }

    public Task Publish(object message, CancellationToken cancellationToken = default)
    {
        return Add(token => _publishEndpoint.Publish(message, token), cancellationToken);
    }

    public Task Publish(object message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
    {
        return Add(token => _publishEndpoint.Publish(message, publishPipe, token), cancellationToken);
    }

    public Task Publish(object message, Type messageType, CancellationToken cancellationToken = default)
    {
        return Add(token => _publishEndpoint.Publish(message, messageType, token), cancellationToken);
    }

    public Task Publish(object message, Type messageType, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
    {
        return Add(token => _publishEndpoint.Publish(message, messageType, publishPipe, token), cancellationToken);
    }

    public Task Publish<T>(object values, CancellationToken cancellationToken = default)
        where T : class
    {
        return Add(token => _publishEndpoint.Publish<T>(values, token), cancellationToken);
    }

    public Task Publish<T>(object values, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default)
        where T : class
    {
        return Add(token => _publishEndpoint.Publish(values, publishPipe, token), cancellationToken);
    }

    public Task Publish<T>(object values, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
        where T : class
    {
        return Add(token => _publishEndpoint.Publish<T>(values, publishPipe, token), cancellationToken);
    }

    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        return _bus.ConnectSendObserver(observer);
    }

    public async Task<ISendEndpoint> GetSendEndpoint(Uri address)
    {
        ISendEndpoint endpoint = await _bus.GetSendEndpoint(address).ConfigureAwait(false);
        return new DeferredBusSendEndpoint(this, endpoint);
    }

    public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe)
        where T : class
    {
        return _bus.ConnectConsumePipe(pipe);
    }

    public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options)
        where T : class
    {
        return _bus.ConnectConsumePipe(pipe, options);
    }

    public ConnectHandle ConnectRequestPipe<T>(Guid requestId, IPipe<ConsumeContext<T>> pipe)
        where T : class
    {
        return _bus.ConnectRequestPipe(requestId, pipe);
    }

    public ConnectHandle ConnectConsumeMessageObserver<T>(IConsumeMessageObserver<T> observer)
        where T : class
    {
        return _bus.ConnectConsumeMessageObserver(observer);
    }

    public ConnectHandle ConnectConsumeObserver(IConsumeObserver observer)
    {
        return _bus.ConnectConsumeObserver(observer);
    }

    public ConnectHandle ConnectReceiveObserver(IReceiveObserver observer)
    {
        return _bus.ConnectReceiveObserver(observer);
    }

    public ConnectHandle ConnectReceiveEndpointObserver(IReceiveEndpointObserver observer)
    {
        return _bus.ConnectReceiveEndpointObserver(observer);
    }

    public ConnectHandle ConnectEndpointConfigurationObserver(IEndpointConfigurationObserver observer)
    {
        return _bus.ConnectEndpointConfigurationObserver(observer);
    }

    public HostReceiveEndpointHandle ConnectReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter endpointNameFormatter,
        Action<IReceiveEndpointConfigurator> configureEndpoint = null)
    {
        return _bus.ConnectReceiveEndpoint(definition, endpointNameFormatter, configureEndpoint);
    }

    public HostReceiveEndpointHandle ConnectReceiveEndpoint(string queueName, Action<IReceiveEndpointConfigurator> configureEndpoint)
    {
        return _bus.ConnectReceiveEndpoint(queueName, configureEndpoint);
    }

    public void Probe(ProbeContext context)
    {
        _bus.Probe(context);
    }

    public Uri Address => _bus.Address;
    public IBusTopology Topology => _bus.Topology;

    internal abstract Task Add(Func<CancellationToken, Task> action, CancellationToken cancellationToken);
}
