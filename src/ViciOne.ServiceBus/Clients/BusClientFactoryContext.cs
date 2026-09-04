using System;

#nullable enable
namespace ViciOne.ServiceBus.Clients;

public class BusClientFactoryContext :
    ClientFactoryContext
{
    readonly IBus _bus;

    public BusClientFactoryContext(IBus bus, RequestTimeout defaultTimeout = default, TimeProvider? timeProvider = null)
    {
        _bus = bus;

        DefaultTimeout = defaultTimeout.HasValue ? defaultTimeout : RequestTimeout.Default;
        TimeProvider = timeProvider ?? TimeProvider.System;
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

    public Uri ResponseAddress => _bus.Address;

    public IRequestSendEndpoint<T> GetRequestEndpoint<T>(ConsumeContext? consumeContext = default)
        where T : class
    {
        return new PublishRequestSendEndpoint<T>(_bus, consumeContext);
    }

    public IRequestSendEndpoint<T> GetRequestEndpoint<T>(Uri destinationAddress, ConsumeContext? consumeContext = default)
        where T : class
    {
        return new SendRequestSendEndpoint<T>(_bus, destinationAddress, consumeContext);
    }

    public RequestTimeout DefaultTimeout { get; }

    public IMessageRouteTable MessageRoutes => EndpointConvention.GetMessageRoutes(_bus);

    public TimeProvider TimeProvider { get; }
}
