using System;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Mediator.Contexts;

/// <summary>Connects request clients to the mediator request and response endpoints.</summary>
internal sealed class MediatorClientFactoryContext :
    ClientFactoryContext
{
    readonly IConsumePipe _connector;
    readonly MediatorSendEndpoint _endpoint;

    /// <summary>Initializes request-client routing for an in-process mediator.</summary>
    /// <param name="endpoint">The mediator send endpoint.</param>
    /// <param name="connector">The response consume-pipe connector.</param>
    /// <param name="responseAddress">The mediator response endpoint address.</param>
    /// <param name="defaultTimeout">The default request timeout.</param>
    /// <param name="timeProvider">The time source used for request deadlines.</param>
    public MediatorClientFactoryContext(
        MediatorSendEndpoint endpoint,
        IConsumePipe connector,
        Uri responseAddress,
        RequestTimeout defaultTimeout = default,
        TimeProvider? timeProvider = null)
    {
        _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        _connector = connector ?? throw new ArgumentNullException(nameof(connector));

        ResponseAddress = responseAddress ?? throw new ArgumentNullException(nameof(responseAddress));
        DefaultTimeout = defaultTimeout.Or(RequestTimeout.Default);
        TimeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe)
        where T : class
    {
        return _connector.ConnectConsumePipe(pipe);
    }

    /// <inheritdoc />
    public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options)
        where T : class
    {
        return _connector.ConnectConsumePipe(pipe, options);
    }

    /// <inheritdoc />
    public ConnectHandle ConnectRequestPipe<T>(Guid requestId, IPipe<ConsumeContext<T>> pipe)
        where T : class
    {
        return _connector.ConnectRequestPipe(requestId, pipe);
    }

    /// <inheritdoc />
    public Uri ResponseAddress { get; }

    /// <inheritdoc />
    public IRequestSendEndpoint<T> GetRequestEndpoint<T>(ConsumeContext? consumeContext = default)
        where T : class
    {
        return new MediatorRequestSendEndpoint<T>(_endpoint, consumeContext);
    }

    /// <inheritdoc />
    public IRequestSendEndpoint<T> GetRequestEndpoint<T>(Uri destinationAddress, ConsumeContext? consumeContext = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(destinationAddress);
        return new MediatorRequestSendEndpoint<T>(_endpoint.GetSendEndpoint(destinationAddress), consumeContext);
    }

    /// <inheritdoc />
    public RequestTimeout DefaultTimeout { get; }

    /// <inheritdoc />
    public IMessageRouteTable MessageRoutes => MessageRouteTable.Empty;

    /// <inheritdoc />
    public TimeProvider TimeProvider { get; }
}
