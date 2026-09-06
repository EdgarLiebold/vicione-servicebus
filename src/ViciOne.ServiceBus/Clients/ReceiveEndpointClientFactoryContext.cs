using System;

namespace ViciOne.ServiceBus.Clients;

/// <summary>
/// Provides a receive endpoint client factory context implementation.
/// </summary>
public class ReceiveEndpointClientFactoryContext :
    ClientFactoryContext
{
    readonly HostReceiveEndpointHandle _handle;
    readonly IReceiveEndpoint _receiveEndpoint;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="handle">The handle value.</param>
    /// <param name="defaultTimeout">The default timeout value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    public ReceiveEndpointClientFactoryContext(
        HostReceiveEndpointHandle handle,
        RequestTimeout defaultTimeout = default,
        TimeProvider? timeProvider = null)
    {
        _handle = handle;
        _receiveEndpoint = handle.ReceiveEndpoint;

        ResponseAddress = _receiveEndpoint.InputAddress;

        DefaultTimeout = defaultTimeout.Or(RequestTimeout.Default);
        TimeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Connects consume pipe.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="pipe">The pipe value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe)
        where T : class
    {
        return _receiveEndpoint.ConnectConsumePipe(pipe);
    }

    /// <summary>
    /// Connects consume pipe.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="options">The options value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options)
        where T : class
    {
        return _receiveEndpoint.ConnectConsumePipe(pipe, options);
    }

    /// <summary>
    /// Connects request pipe.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="requestId">The request id value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectRequestPipe<T>(Guid requestId, IPipe<ConsumeContext<T>> pipe)
        where T : class
    {
        return _receiveEndpoint.ConnectRequestPipe(requestId, pipe);
    }

    /// <summary>
    /// Gets the response address value.
    /// </summary>
    public Uri ResponseAddress { get; }

    /// <summary>
    /// Gets request endpoint.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="consumeContext">The consume context value.</param>
    /// <returns>The result of the operation.</returns>
    public IRequestSendEndpoint<T> GetRequestEndpoint<T>(ConsumeContext? consumeContext = default)
        where T : class
    {
        return new ReceiveEndpointPublishRequestSendEndpoint<T>(_handle, consumeContext);
    }

    /// <summary>
    /// Gets request endpoint.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="destinationAddress">The destination address value.</param>
    /// <param name="consumeContext">The consume context value.</param>
    /// <returns>The result of the operation.</returns>
    public IRequestSendEndpoint<T> GetRequestEndpoint<T>(Uri destinationAddress, ConsumeContext? consumeContext = default)
        where T : class
    {
        return new ReceiveEndpointSendRequestSendEndpoint<T>(_handle, destinationAddress, consumeContext);
    }

    /// <summary>
    /// Gets the default timeout value.
    /// </summary>
    public RequestTimeout DefaultTimeout { get; }

    /// <summary>
    /// Gets the message routes value.
    /// </summary>
    public IMessageRouteTable MessageRoutes => EndpointConvention.GetMessageRoutes(_receiveEndpoint);

    /// <summary>
    /// Gets the time provider value.
    /// </summary>
    public TimeProvider TimeProvider { get; }
}
