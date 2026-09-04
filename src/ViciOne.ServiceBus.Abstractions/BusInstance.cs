using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

/// <summary>
/// When configuring multiple bus instances in a single container (MultiBus), this base class should be used
/// as a the base for the additional bus instance type.
/// </summary>
/// <typeparam name="TBus">The specific bus interface type for this bus instance</typeparam>
// ReSharper disable once UnusedTypeParameter
public abstract class BusInstance<TBus> :
    IBusControl,
    Advanced.IAdvancedPublishEndpoint,
    IMessageRouteProvider
    where TBus : class, IBus
{
    readonly IBusControl _busControl;

    protected BusInstance(IBusControl busControl)
    {
        _busControl = busControl;
    }

    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        return _busControl.ConnectPublishObserver(observer);
    }

    public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class
    {
        return _busControl.PublishAsync(message, cancellationToken);
    }

    public Task PublishAsync<T>(T message, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default)
        where T : class
    {
        return _busControl.PublishAsync(message, publishPipe, cancellationToken);
    }

    public Task PublishAsync<T>(T message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
        where T : class
    {
        return _busControl.PublishAsync(message, publishPipe, cancellationToken);
    }

    public Task PublishAsync(object message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        return _busControl.Advanced().PublishAsync(message, message.GetType(), cancellationToken);
    }

    public Task PublishAsync(object message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        return _busControl.Advanced().PublishAsync(message, message.GetType(), publishPipe, cancellationToken);
    }

    public Task PublishAsync(object message, Type messageType, CancellationToken cancellationToken = default)
    {
        return _busControl.PublishAsync(message, messageType, cancellationToken);
    }

    public Task PublishAsync(object message, Type messageType, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
    {
        return _busControl.PublishAsync(message, messageType, publishPipe, cancellationToken);
    }

    public Task PublishAsync<T>(object values, CancellationToken cancellationToken = default)
        where T : class
    {
        return _busControl.PublishAsync<T>(values, cancellationToken);
    }

    public Task PublishAsync<T>(object values, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default)
        where T : class
    {
        return _busControl.PublishAsync(values, publishPipe, cancellationToken);
    }

    public Task PublishAsync<T>(object values, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
        where T : class
    {
        return _busControl.PublishAsync<T>(values, publishPipe, cancellationToken);
    }

    public Task<ISendEndpoint> GetPublishSendEndpointAsync<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        return _busControl.GetPublishSendEndpointAsync<T>(cancellationToken: cancellationToken);
    }

    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        return _busControl.ConnectSendObserver(observer);
    }

    public Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default)
    {
        return _busControl.GetSendEndpointAsync(address, cancellationToken: cancellationToken);
    }

    public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe)
        where T : class
    {
        return _busControl.ConnectConsumePipe(pipe);
    }

    public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options)
        where T : class
    {
        return _busControl.ConnectConsumePipe(pipe, options);
    }

    public ConnectHandle ConnectRequestPipe<T>(Guid requestId, IPipe<ConsumeContext<T>> pipe)
        where T : class
    {
        return _busControl.ConnectRequestPipe(requestId, pipe);
    }

    public ConnectHandle ConnectConsumeMessageObserver<T>(IConsumeMessageObserver<T> observer)
        where T : class
    {
        return _busControl.ConnectConsumeMessageObserver(observer);
    }

    public ConnectHandle ConnectConsumeObserver(IConsumeObserver observer)
    {
        return _busControl.ConnectConsumeObserver(observer);
    }

    public ConnectHandle ConnectReceiveObserver(IReceiveObserver observer)
    {
        return _busControl.ConnectReceiveObserver(observer);
    }

    public ConnectHandle ConnectReceiveEndpointObserver(IReceiveEndpointObserver observer)
    {
        return _busControl.ConnectReceiveEndpointObserver(observer);
    }

    public ConnectHandle ConnectEndpointConfigurationObserver(IEndpointConfigurationObserver observer)
    {
        return _busControl.ConnectEndpointConfigurationObserver(observer);
    }

    public HostReceiveEndpointHandle ConnectReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<IReceiveEndpointConfigurator>? configureEndpoint = null)
    {
        return _busControl.ConnectReceiveEndpoint(definition, endpointNameFormatter, configureEndpoint);
    }

    public HostReceiveEndpointHandle ConnectReceiveEndpoint(string queueName, Action<IReceiveEndpointConfigurator>? configureEndpoint)
    {
        return _busControl.ConnectReceiveEndpoint(queueName, configureEndpoint);
    }

    public void Probe(ProbeContext context)
    {
        _busControl.Probe(context);
    }

    public Uri Address => _busControl.Address;

    public IBusTopology Topology => _busControl.Topology;

    IMessageRouteTable IMessageRouteProvider.MessageRoutes => _busControl is IMessageRouteProvider routeProvider
        ? routeProvider.MessageRoutes
        : throw new ConfigurationException(
            $"The wrapped bus control {_busControl.GetType().Name} does not expose its message routes.");

    public Task<BusHandle> StartAsync(CancellationToken cancellationToken = default)
    {
        return _busControl.StartAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        return _busControl.StopAsync(cancellationToken);
    }

    public BusHealthResult CheckHealth()
    {
        return _busControl.CheckHealth();
    }
}
