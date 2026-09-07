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

    /// <summary>Connects consume pipe.</summary>
    /// <typeparam name="T">The response message contract.</typeparam>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe)
        where T : class
    {
        return _connector.ConnectConsumePipe(pipe);
    }

    /// <summary>Connects consume pipe.</summary>
    /// <typeparam name="T">The response message contract.</typeparam>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options)
        where T : class
    {
        return _connector.ConnectConsumePipe(pipe, options);
    }

    /// <summary>Connects request pipe.</summary>
    /// <typeparam name="T">The response message contract.</typeparam>
    /// <param name="requestId">The request id.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectRequestPipe<T>(Guid requestId, IPipe<ConsumeContext<T>> pipe)
        where T : class
    {
        return _connector.ConnectRequestPipe(requestId, pipe);
    }

    /// <summary>Gets the response address.</summary>
    public Uri ResponseAddress { get; }

    /// <summary>Gets request endpoint.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="consumeContext">The consume context.</param>
    /// <returns>The request endpoint.</returns>
    public IRequestSendEndpoint<T> GetRequestEndpoint<T>(ConsumeContext? consumeContext = default)
        where T : class
    {
        return new MediatorRequestSendEndpoint<T>(_endpoint, consumeContext);
    }

    /// <summary>Gets request endpoint.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="consumeContext">The consume context.</param>
    /// <returns>The request endpoint.</returns>
    public IRequestSendEndpoint<T> GetRequestEndpoint<T>(Uri destinationAddress, ConsumeContext? consumeContext = default)
        where T : class
    {
        return new MediatorRequestSendEndpoint<T>(_endpoint.GetSendEndpoint(destinationAddress), consumeContext);
    }

    /// <summary>Gets the default timeout.</summary>
    public RequestTimeout DefaultTimeout { get; }

    /// <summary>Gets the message routes.</summary>
    public IMessageRouteTable MessageRoutes => MessageRouteTable.Empty;

    /// <summary>Gets the time provider.</summary>
    public TimeProvider TimeProvider { get; }
}
