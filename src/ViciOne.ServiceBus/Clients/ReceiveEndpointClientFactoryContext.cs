using System;

namespace ViciOne.ServiceBus.Clients;

/// <summary>Carries state for receive endpoint client factory operations.</summary>
public class ReceiveEndpointClientFactoryContext :
    ClientFactoryContext
{
    readonly HostReceiveEndpointHandle _handle;
    readonly IReceiveEndpoint _receiveEndpoint;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="handle">The handle.</param>
    /// <param name="defaultTimeout">The default timeout.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
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

    /// <summary>Connects consume pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe)
        where T : class
    {
        return _receiveEndpoint.ConnectConsumePipe(pipe);
    }

    /// <summary>Connects consume pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options)
        where T : class
    {
        return _receiveEndpoint.ConnectConsumePipe(pipe, options);
    }

    /// <summary>Connects request pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="requestId">The request id.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectRequestPipe<T>(Guid requestId, IPipe<ConsumeContext<T>> pipe)
        where T : class
    {
        return _receiveEndpoint.ConnectRequestPipe(requestId, pipe);
    }

    /// <summary>Gets the response address.</summary>
    public Uri ResponseAddress { get; }

    /// <summary>Gets request endpoint.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="consumeContext">The consume context.</param>
    /// <returns>The request endpoint.</returns>
    public IRequestSendEndpoint<T> GetRequestEndpoint<T>(ConsumeContext? consumeContext = default)
        where T : class
    {
        return new ReceiveEndpointPublishRequestSendEndpoint<T>(_handle, consumeContext);
    }

    /// <summary>Gets request endpoint.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="consumeContext">The consume context.</param>
    /// <returns>The request endpoint.</returns>
    public IRequestSendEndpoint<T> GetRequestEndpoint<T>(Uri destinationAddress, ConsumeContext? consumeContext = default)
        where T : class
    {
        return new ReceiveEndpointSendRequestSendEndpoint<T>(_handle, destinationAddress, consumeContext);
    }

    /// <summary>Gets the default timeout.</summary>
    public RequestTimeout DefaultTimeout { get; }

    /// <summary>Gets the message routes.</summary>
    public IMessageRouteTable MessageRoutes => EndpointConvention.GetMessageRoutes(_receiveEndpoint);

    /// <summary>Gets the time provider.</summary>
    public TimeProvider TimeProvider { get; }
}
