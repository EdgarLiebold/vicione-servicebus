using System;

#nullable enable
namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// Provides a scoped client factory context implementation.
/// </summary>
public class ScopedClientFactoryContext :
    ClientFactoryContext
{
    readonly IClientFactory _clientFactory;
    readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="clientFactory">The client factory value.</param>
    /// <param name="serviceProvider">The service provider value.</param>
    public ScopedClientFactoryContext(IClientFactory clientFactory, IServiceProvider serviceProvider)
    {
        _clientFactory = clientFactory;
        _serviceProvider = serviceProvider;
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
        return _clientFactory.Context.ConnectConsumePipe(pipe);
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
        return _clientFactory.Context.ConnectConsumePipe(pipe, options);
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
        return _clientFactory.Context.ConnectRequestPipe(requestId, pipe);
    }

    /// <summary>
    /// Gets the default timeout value.
    /// </summary>
    public RequestTimeout DefaultTimeout => _clientFactory.Context.DefaultTimeout;

    /// <summary>
    /// Gets the message routes value.
    /// </summary>
    public IMessageRouteTable MessageRoutes => _clientFactory.Context.MessageRoutes;

    /// <summary>
    /// Gets the time provider value.
    /// </summary>
    public TimeProvider TimeProvider => _clientFactory.Context.TimeProvider;

    /// <summary>
    /// Gets the response address value.
    /// </summary>
    public Uri ResponseAddress => _clientFactory.Context.ResponseAddress;

    /// <summary>
    /// Gets request endpoint.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="consumeContext">The consume context value.</param>
    /// <returns>The result of the operation.</returns>
    public IRequestSendEndpoint<T> GetRequestEndpoint<T>(ConsumeContext? consumeContext = default)
        where T : class
    {
        IRequestSendEndpoint<T> endpoint = _clientFactory.Context.GetRequestEndpoint<T>(consumeContext);
        return new ScopedRequestSendEndpoint<T>(endpoint, _serviceProvider);
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
        IRequestSendEndpoint<T> endpoint = _clientFactory.Context.GetRequestEndpoint<T>(destinationAddress, consumeContext);
        return new ScopedRequestSendEndpoint<T>(endpoint, _serviceProvider);
    }
}
