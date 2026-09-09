using System;

namespace ViciOne.ServiceBus.Clients;

/// <summary>Connects request clients directly to a bus instance.</summary>
internal sealed class BusClientFactoryContext :
    ClientFactoryContext
{
    readonly IBus _bus;

    /// <summary>Creates a request-client context backed by a started bus.</summary>
    /// <param name="bus">The bus that sends requests and receives responses.</param>
    /// <param name="defaultTimeout">The default request timeout.</param>
    /// <param name="timeProvider">The time source used to measure request deadlines.</param>
    public BusClientFactoryContext(IBus bus, RequestTimeout defaultTimeout = default, TimeProvider? timeProvider = null)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));

        DefaultTimeout = defaultTimeout.HasValue ? defaultTimeout : RequestTimeout.Default;
        TimeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>Connects a response pipeline for a message contract.</summary>
    /// <typeparam name="T">The response message contract.</typeparam>
    /// <param name="pipe">The pipeline invoked for matching responses.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe)
        where T : class
    {
        return _bus.ConnectConsumePipe(pipe);
    }

    /// <summary>Connects a configurable response pipeline for a message contract.</summary>
    /// <typeparam name="T">The response message contract.</typeparam>
    /// <param name="pipe">The pipeline invoked for matching responses.</param>
    /// <param name="options">The settings that control pipe connection and scheduling.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options)
        where T : class
    {
        return _bus.ConnectConsumePipe(pipe, options);
    }

    /// <summary>Connects a response pipeline for one request correlation identifier.</summary>
    /// <typeparam name="T">The response message contract.</typeparam>
    /// <param name="requestId">The request correlation identifier.</param>
    /// <param name="pipe">The pipeline invoked for matching responses.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectRequestPipe<T>(Guid requestId, IPipe<ConsumeContext<T>> pipe)
        where T : class
    {
        return _bus.ConnectRequestPipe(requestId, pipe);
    }

    /// <summary>Gets the bus address used for responses.</summary>
    public Uri ResponseAddress => _bus.Address;

    /// <summary>Creates a request endpoint that publishes requests through the bus.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="consumeContext">The consume context whose correlation metadata is propagated, or <see langword="null" />.</param>
    /// <returns>The publish-backed request endpoint.</returns>
    public IRequestSendEndpoint<T> GetRequestEndpoint<T>(ConsumeContext? consumeContext = default)
        where T : class
    {
        return new PublishRequestSendEndpoint<T>(_bus, consumeContext);
    }

    /// <summary>Creates a request endpoint for an explicit destination.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="destinationAddress">The request service address.</param>
    /// <param name="consumeContext">The consume context whose correlation metadata is propagated, or <see langword="null" />.</param>
    /// <returns>The send-backed request endpoint.</returns>
    public IRequestSendEndpoint<T> GetRequestEndpoint<T>(Uri destinationAddress, ConsumeContext? consumeContext = default)
        where T : class
    {
        return new SendRequestSendEndpoint<T>(_bus, destinationAddress, consumeContext);
    }

    /// <summary>Gets the default time limit for requests.</summary>
    public RequestTimeout DefaultTimeout { get; }

    /// <summary>Gets the message routes owned by the bus.</summary>
    public IMessageRouteTable MessageRoutes => EndpointConvention.GetMessageRoutes(_bus);

    /// <summary>Gets the time source used to measure request deadlines.</summary>
    public TimeProvider TimeProvider { get; }
}
