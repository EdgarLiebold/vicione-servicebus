using System;

namespace ViciOne.ServiceBus.Clients;

/// <summary>Creates request endpoints that receive responses through a connected receive endpoint.</summary>
internal class ReceiveEndpointClientFactoryContext :
    ClientFactoryContext
{
    readonly IHostReceiveEndpointHandle _handle;
    readonly IReceiveEndpoint _receiveEndpoint;

    /// <summary>Creates a client-factory context for a connected response endpoint.</summary>
    /// <param name="handle">The connected endpoint used to receive responses and resolve destinations.</param>
    /// <param name="defaultTimeout">The default request timeout.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    public ReceiveEndpointClientFactoryContext(
        IHostReceiveEndpointHandle handle,
        RequestTimeout defaultTimeout = default,
        TimeProvider? timeProvider = null)
    {
        _handle = handle ?? throw new ArgumentNullException(nameof(handle));
        _receiveEndpoint = handle.ReceiveEndpoint;

        ResponseAddress = _receiveEndpoint.InputAddress;

        DefaultTimeout = defaultTimeout.Or(RequestTimeout.Default);
        TimeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>Connects a response pipeline for a message contract.</summary>
    /// <typeparam name="T">The response message contract.</typeparam>
    /// <param name="pipe">The pipeline invoked for matching responses.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe)
        where T : class
    {
        return _receiveEndpoint.ConnectConsumePipe(pipe);
    }

    /// <summary>Connects a configurable response pipeline for a message contract.</summary>
    /// <typeparam name="T">The response message contract.</typeparam>
    /// <param name="pipe">The pipeline invoked for matching responses.</param>
    /// <param name="options">The settings that control pipe connection and scheduling.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options)
        where T : class
    {
        return _receiveEndpoint.ConnectConsumePipe(pipe, options);
    }

    /// <summary>Connects a response pipeline for one request correlation identifier.</summary>
    /// <typeparam name="T">The response message contract.</typeparam>
    /// <param name="requestId">The request correlation identifier.</param>
    /// <param name="pipe">The pipeline invoked for matching responses.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectRequestPipe<T>(Guid requestId, IPipe<ConsumeContext<T>> pipe)
        where T : class
    {
        return _receiveEndpoint.ConnectRequestPipe(requestId, pipe);
    }

    /// <summary>Gets the input address of the connected endpoint that receives responses.</summary>
    public Uri ResponseAddress { get; }

    /// <summary>Creates a request endpoint that publishes requests.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="consumeContext">The consume context whose request metadata is propagated, or <see langword="null" />.</param>
    /// <returns>The publish-backed request endpoint.</returns>
    public IRequestSendEndpoint<T> GetRequestEndpoint<T>(ConsumeContext? consumeContext = default)
        where T : class
    {
        return new ReceiveEndpointPublishRequestSendEndpoint<T>(_handle, consumeContext);
    }

    /// <summary>Creates a request endpoint that sends to an explicit destination.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="destinationAddress">The request service address.</param>
    /// <param name="consumeContext">The consume context whose request metadata is propagated, or <see langword="null" />.</param>
    /// <returns>The send-backed request endpoint.</returns>
    public IRequestSendEndpoint<T> GetRequestEndpoint<T>(Uri destinationAddress, ConsumeContext? consumeContext = default)
        where T : class
    {
        return new ReceiveEndpointSendRequestSendEndpoint<T>(_handle, destinationAddress, consumeContext);
    }

    /// <summary>Gets the default time limit applied when a request does not override it.</summary>
    public RequestTimeout DefaultTimeout { get; }

    /// <summary>Gets the message routes available through the connected receive endpoint.</summary>
    public IMessageRouteTable MessageRoutes => EndpointConvention.GetMessageRoutes(_receiveEndpoint);

    /// <summary>Gets the time source used to measure request deadlines.</summary>
    public TimeProvider TimeProvider { get; }
}
