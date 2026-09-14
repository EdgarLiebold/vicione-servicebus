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
        ArgumentNullException.ThrowIfNull(observer);
        return _bus.ConnectPublishObserver(observer);
    }

    public Task<ISendEndpoint> GetPublishSendEndpointAsync<TMessage>(CancellationToken cancellationToken = default)
        where TMessage : class
    {
        return _publishEndpointProvider.GetPublishSendEndpointAsync<TMessage>(cancellationToken: cancellationToken);
    }

    public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);
        return AddAsync(token => _publishEndpoint.PublishAsync(message, token), cancellationToken);
    }

    public Task PublishAsync<TMessage>(TMessage message, IPipe<PublishContext<TMessage>> publishPipe, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(publishPipe);
        return AddAsync(token => _publishEndpoint.PublishAsync(message, publishPipe, token), cancellationToken);
    }

    public Task PublishAsync<TMessage>(TMessage message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(publishPipe);
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
        ArgumentNullException.ThrowIfNull(publishPipe);
        return AddAsync(token => _publishEndpoint.Advanced().PublishAsync(message, message.GetType(), publishPipe, token), cancellationToken);
    }

    public Task PublishAsync(object message, Type messageType, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);
        return AddAsync(token => _publishEndpoint.PublishAsync(message, messageType, token), cancellationToken);
    }

    public Task PublishAsync(object message, Type messageType, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentNullException.ThrowIfNull(publishPipe);
        return AddAsync(token => _publishEndpoint.PublishAsync(message, messageType, publishPipe, token), cancellationToken);
    }

    public Task PublishAsync<TMessage>(object values, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(values);
        return AddAsync(token => _publishEndpoint.PublishAsync<TMessage>(values, token), cancellationToken);
    }

    public Task PublishAsync<TMessage>(object values, IPipe<PublishContext<TMessage>> publishPipe, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(publishPipe);
        return AddAsync(token => _publishEndpoint.PublishAsync(values, publishPipe, token), cancellationToken);
    }

    public Task PublishAsync<TMessage>(object values, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(publishPipe);
        return AddAsync(token => _publishEndpoint.PublishAsync<TMessage>(values, publishPipe, token), cancellationToken);
    }

    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _bus.ConnectSendObserver(observer);
    }

    public async Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(address);
        ISendEndpoint endpoint = await _bus.GetSendEndpointAsync(address, cancellationToken: cancellationToken).ConfigureAwait(false);
        return new DeferredBusSendEndpoint(this, endpoint);
    }

    public ConnectHandle ConnectConsumePipe<TMessage>(IPipe<ConsumeContext<TMessage>> pipe)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(pipe);
        return _bus.ConnectConsumePipe(pipe);
    }

    public ConnectHandle ConnectConsumePipe<TMessage>(IPipe<ConsumeContext<TMessage>> pipe, ConnectPipeOptions options)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(pipe);
        return _bus.ConnectConsumePipe(pipe, options);
    }

    public ConnectHandle ConnectRequestPipe<TMessage>(Guid requestId, IPipe<ConsumeContext<TMessage>> pipe)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(pipe);
        return _bus.ConnectRequestPipe(requestId, pipe);
    }

    public ConnectHandle ConnectConsumeMessageObserver<TMessage>(IConsumeMessageObserver<TMessage> observer)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _bus.ConnectConsumeMessageObserver(observer);
    }

    public ConnectHandle ConnectConsumeObserver(IConsumeObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _bus.ConnectConsumeObserver(observer);
    }

    public ConnectHandle ConnectReceiveObserver(IReceiveObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _bus.ConnectReceiveObserver(observer);
    }

    public ConnectHandle ConnectReceiveEndpointObserver(IReceiveEndpointObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _bus.ConnectReceiveEndpointObserver(observer);
    }

    public ConnectHandle ConnectEndpointConfigurationObserver(IEndpointConfigurationObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _bus.ConnectEndpointConfigurationObserver(observer);
    }

    public IHostReceiveEndpointHandle ConnectReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<IReceiveEndpointConfigurator>? configureEndpoint = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return _bus.ConnectReceiveEndpoint(definition, endpointNameFormatter, configureEndpoint);
    }

    public IHostReceiveEndpointHandle ConnectReceiveEndpoint(string queueName, Action<IReceiveEndpointConfigurator>? configureEndpoint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        return _bus.ConnectReceiveEndpoint(queueName, configureEndpoint);
    }

    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _bus.Probe(context);
    }

    public Uri Address => _bus.Address;
    public IBusTopology Topology => _bus.Topology;

    internal abstract Task AddAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken);
}
