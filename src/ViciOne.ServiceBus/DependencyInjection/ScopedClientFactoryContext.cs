using System;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Carries state for scoped client factory operations.</summary>
public class ScopedClientFactoryContext :
    ClientFactoryContext
{
    readonly IClientFactory _clientFactory;
    readonly IServiceProvider _serviceProvider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="clientFactory">The client factory.</param>
    /// <param name="serviceProvider">The service provider.</param>
    public ScopedClientFactoryContext(IClientFactory clientFactory, IServiceProvider serviceProvider)
    {
        _clientFactory = clientFactory;
        _serviceProvider = serviceProvider;
    }

    /// <summary>Connects consume pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe)
        where T : class
    {
        return _clientFactory.Context.ConnectConsumePipe(pipe);
    }

    /// <summary>Connects consume pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options)
        where T : class
    {
        return _clientFactory.Context.ConnectConsumePipe(pipe, options);
    }

    /// <summary>Connects request pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="requestId">The request id.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectRequestPipe<T>(Guid requestId, IPipe<ConsumeContext<T>> pipe)
        where T : class
    {
        return _clientFactory.Context.ConnectRequestPipe(requestId, pipe);
    }

    /// <summary>Gets the default timeout.</summary>
    public RequestTimeout DefaultTimeout => _clientFactory.Context.DefaultTimeout;

    /// <summary>Gets the message routes.</summary>
    public IMessageRouteTable MessageRoutes => _clientFactory.Context.MessageRoutes;

    /// <summary>Gets the time provider.</summary>
    public TimeProvider TimeProvider => _clientFactory.Context.TimeProvider;

    /// <summary>Gets the response address.</summary>
    public Uri ResponseAddress => _clientFactory.Context.ResponseAddress;

    /// <summary>Gets request endpoint.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="consumeContext">The consume context.</param>
    /// <returns>The request endpoint.</returns>
    public IRequestSendEndpoint<T> GetRequestEndpoint<T>(ConsumeContext? consumeContext = default)
        where T : class
    {
        IRequestSendEndpoint<T> endpoint = _clientFactory.Context.GetRequestEndpoint<T>(consumeContext);
        return new ScopedRequestSendEndpoint<T>(endpoint, _serviceProvider);
    }

    /// <summary>Gets request endpoint.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="consumeContext">The consume context.</param>
    /// <returns>The request endpoint.</returns>
    public IRequestSendEndpoint<T> GetRequestEndpoint<T>(Uri destinationAddress, ConsumeContext? consumeContext = default)
        where T : class
    {
        IRequestSendEndpoint<T> endpoint = _clientFactory.Context.GetRequestEndpoint<T>(destinationAddress, consumeContext);
        return new ScopedRequestSendEndpoint<T>(endpoint, _serviceProvider);
    }
}
