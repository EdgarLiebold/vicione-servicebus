using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Transactions;

internal abstract class DeferredBus :
    IBus,
    Advanced.IAdvancedPublishEndpoint
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

    public Task<ISendEndpoint> GetPublishSendEndpointAsync<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        return _publishEndpointProvider.GetPublishSendEndpointAsync<T>(cancellationToken: cancellationToken);
    }

    public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class
    {
        return AddAsync(token => _publishEndpoint.PublishAsync(message, token), cancellationToken);
    }

    public Task PublishAsync<T>(T message, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default)
        where T : class
    {
        return AddAsync(token => _publishEndpoint.PublishAsync(message, publishPipe, token), cancellationToken);
    }

    public Task PublishAsync<T>(T message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
        where T : class
    {
        return AddAsync(token => _publishEndpoint.PublishAsync(message, publishPipe, token), cancellationToken);
    }

    public Task PublishAsync(object message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        return AddAsync(token => _publishEndpoint.Advanced().PublishAsync(message, message.GetType(), token), cancellationToken);
    }

    public Task PublishAsync(object message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        return AddAsync(token => _publishEndpoint.Advanced().PublishAsync(message, message.GetType(), publishPipe, token), cancellationToken);
    }

    public Task PublishAsync(object message, Type messageType, CancellationToken cancellationToken = default)
    {
        return AddAsync(token => _publishEndpoint.PublishAsync(message, messageType, token), cancellationToken);
    }

    public Task PublishAsync(object message, Type messageType, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
    {
        return AddAsync(token => _publishEndpoint.PublishAsync(message, messageType, publishPipe, token), cancellationToken);
    }

    public Task PublishAsync<T>(object values, CancellationToken cancellationToken = default)
        where T : class
    {
        return AddAsync(token => _publishEndpoint.PublishAsync<T>(values, token), cancellationToken);
    }

    public Task PublishAsync<T>(object values, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default)
        where T : class
    {
        return AddAsync(token => _publishEndpoint.PublishAsync(values, publishPipe, token), cancellationToken);
    }

    public Task PublishAsync<T>(object values, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
        where T : class
    {
        return AddAsync(token => _publishEndpoint.PublishAsync<T>(values, publishPipe, token), cancellationToken);
    }

    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        return _bus.ConnectSendObserver(observer);
    }

    public async Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default)
    {
        ISendEndpoint endpoint = await _bus.GetSendEndpointAsync(address, cancellationToken: cancellationToken).ConfigureAwait(false);
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

    public IHostReceiveEndpointHandle ConnectReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<IReceiveEndpointConfigurator>? configureEndpoint = null)
    {
        return _bus.ConnectReceiveEndpoint(definition, endpointNameFormatter, configureEndpoint);
    }

    public IHostReceiveEndpointHandle ConnectReceiveEndpoint(string queueName, Action<IReceiveEndpointConfigurator>? configureEndpoint)
    {
        return _bus.ConnectReceiveEndpoint(queueName, configureEndpoint);
    }

    public void Probe(ProbeContext context)
    {
        _bus.Probe(context);
    }

    public Uri Address => _bus.Address;
    public IBusTopology Topology => _bus.Topology;

    internal abstract Task AddAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken);
}
