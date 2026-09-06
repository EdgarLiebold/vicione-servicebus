using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Clients;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Mediator.Contexts;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Mediator;

/// <summary>
/// Sends messages directly to the <see cref="IReceivePipe" />, without serialization
/// </summary>
public class ViciOneServiceBusMediator :
    IMediator,
    Advanced.IAdvancedSendEndpoint,
    Advanced.IAdvancedPublishEndpoint,
    IAsyncDisposable
{
    readonly ClientFactory _clientFactory;
    readonly IReceivePipeDispatcher _dispatcher;
    readonly MediatorSendEndpoint _endpoint;
    readonly IReceivePipeDispatcher _responseDispatcher;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="logContext">The log context value.</param>
    /// <param name="configuration">The configuration callback.</param>
    /// <param name="dispatcher">The dispatcher value.</param>
    /// <param name="responseConfiguration">The response configuration value.</param>
    /// <param name="responseDispatcher">The response dispatcher value.</param>
    /// <param name="limits">The mandatory mediator message limits.</param>
    public ViciOneServiceBusMediator(ILogContext logContext, IReceiveEndpointConfiguration configuration, IReceivePipeDispatcher dispatcher,
        IReceiveEndpointConfiguration responseConfiguration, IReceivePipeDispatcher responseDispatcher, MessageLimits limits)
        : this(logContext, configuration, dispatcher, responseConfiguration, responseDispatcher, limits, TimeProvider.System)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="logContext">The log context value.</param>
    /// <param name="configuration">The configuration callback.</param>
    /// <param name="dispatcher">The dispatcher value.</param>
    /// <param name="responseConfiguration">The response configuration value.</param>
    /// <param name="responseDispatcher">The response dispatcher value.</param>
    /// <param name="limits">The mandatory mediator message limits.</param>
    /// <param name="timeProvider">The time provider value.</param>
    public ViciOneServiceBusMediator(
        ILogContext? logContext,
        IReceiveEndpointConfiguration configuration,
        IReceivePipeDispatcher dispatcher,
        IReceiveEndpointConfiguration responseConfiguration,
        IReceivePipeDispatcher responseDispatcher,
        MessageLimits limits,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(limits);
        if (timeProvider == null)
            throw new ArgumentNullException(nameof(timeProvider));

        _responseDispatcher = responseDispatcher;
        _dispatcher = dispatcher;
        var sendObservable = new SendObservable();

        _endpoint = new MediatorSendEndpoint(
            configuration,
            dispatcher,
            logContext,
            sendObservable,
            responseConfiguration,
            responseDispatcher,
            limits);

        var clientFactoryContext = new MediatorClientFactoryContext(
            _endpoint,
            responseConfiguration.ConsumePipe,
            responseConfiguration.InputAddress,
            timeProvider: timeProvider);
        _clientFactory = new ClientFactory(clientFactoryContext);
    }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public ValueTask DisposeAsync()
    {
        return _clientFactory.DisposeAsync();
    }

    /// <summary>
    /// Connects send observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        return _endpoint.ConnectSendObserver(observer);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync<T>(T message, CancellationToken cancellationToken)
        where T : class
    {
        return _endpoint.SendAsync(message, cancellationToken);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        return _endpoint.SendAsync(message, pipe, cancellationToken);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync<T>(T message, IPipe<SendContext> pipe, CancellationToken cancellationToken)
        where T : class
    {
        return _endpoint.SendAsync(message, pipe, cancellationToken);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(object message, CancellationToken cancellationToken)
    {
        return _endpoint.SendAsync(message, cancellationToken);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="messageType">The message type value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(object message, Type messageType, CancellationToken cancellationToken)
    {
        return _endpoint.SendAsync(message, messageType, cancellationToken);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(object message, IPipe<SendContext> pipe, CancellationToken cancellationToken)
    {
        return _endpoint.SendAsync(message, pipe, cancellationToken);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="messageType">The message type value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(object message, Type messageType, IPipe<SendContext> pipe, CancellationToken cancellationToken)
    {
        return _endpoint.SendAsync(message, messageType, pipe, cancellationToken);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="values">The values value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync<T>(object values, CancellationToken cancellationToken)
        where T : class
    {
        return _endpoint.SendAsync<T>(values, cancellationToken);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="values">The values value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync<T>(object values, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        return _endpoint.SendAsync(values, pipe, cancellationToken);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="values">The values value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync<T>(object values, IPipe<SendContext> pipe, CancellationToken cancellationToken)
        where T : class
    {
        return _endpoint.SendAsync<T>(values, pipe, cancellationToken);
    }

    /// <summary>
    /// Connects publish observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        return _endpoint.ConnectPublishObserver(observer);
    }

    /// <summary>
    /// Gets publish send endpoint.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<ISendEndpoint> GetPublishSendEndpointAsync<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        return _endpoint.GetPublishSendEndpointAsync<T>(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Publishes a message to its configured consumers.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task PublishAsync<T>(T message, CancellationToken cancellationToken)
        where T : class
    {
        return PublishInternalAsync(cancellationToken, message);
    }

    /// <summary>
    /// Publishes a message to its configured consumers.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="publishPipe">The publish pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task PublishAsync<T>(T message, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken)
        where T : class
    {
        return PublishInternalAsync(cancellationToken, message, publishPipe);
    }

    /// <summary>
    /// Publishes a message to its configured consumers.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="publishPipe">The publish pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task PublishAsync<T>(T message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken)
        where T : class
    {
        return PublishInternalAsync(cancellationToken, message, publishPipe);
    }

    /// <summary>
    /// Publishes a message to its configured consumers.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task PublishAsync(object message, CancellationToken cancellationToken)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var messageType = message.GetType();

        return PublishEndpointConverterCache.PublishAsync(this, message, messageType, cancellationToken);
    }

    /// <summary>
    /// Publishes a message to its configured consumers.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="publishPipe">The publish pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task PublishAsync(object message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var messageType = message.GetType();

        return PublishEndpointConverterCache.PublishAsync(this, message, messageType, publishPipe, cancellationToken);
    }

    /// <summary>
    /// Publishes a message to its configured consumers.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="messageType">The message type value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task PublishAsync(object message, Type messageType, CancellationToken cancellationToken)
    {
        return PublishEndpointConverterCache.PublishAsync(this, message, messageType, cancellationToken);
    }

    /// <summary>
    /// Publishes a message to its configured consumers.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="messageType">The message type value.</param>
    /// <param name="publishPipe">The publish pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task PublishAsync(object message, Type messageType, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken)
    {
        return PublishEndpointConverterCache.PublishAsync(this, message, messageType, publishPipe, cancellationToken);
    }

    /// <summary>
    /// Publishes a message to its configured consumers.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="values">The values value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task PublishAsync<T>(object values, CancellationToken cancellationToken)
        where T : class
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        return PublishInternalAsync<T>(cancellationToken, values);
    }

    /// <summary>
    /// Publishes a message to its configured consumers.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="values">The values value.</param>
    /// <param name="publishPipe">The publish pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task PublishAsync<T>(object values, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken)
        where T : class
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        return PublishInternalAsync(cancellationToken, values, publishPipe);
    }

    /// <summary>
    /// Publishes a message to its configured consumers.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="values">The values value.</param>
    /// <param name="publishPipe">The publish pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task PublishAsync<T>(object values, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken)
        where T : class
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        return PublishInternalAsync<T>(cancellationToken, values, publishPipe);
    }

    /// <summary>
    /// Creates request.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public RequestHandle<T> CreateRequest<T>(T message, RequestTimeout timeout, CancellationToken cancellationToken)
        where T : class
    {
        return _clientFactory.CreateRequest(message, timeout, cancellationToken);
    }

    /// <summary>
    /// Creates request.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="destinationAddress">The destination address value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public RequestHandle<T> CreateRequest<T>(Uri destinationAddress, T message, RequestTimeout timeout, CancellationToken cancellationToken)
        where T : class
    {
        return _clientFactory.CreateRequest(destinationAddress, message, timeout, cancellationToken);
    }

    /// <summary>
    /// Creates request.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="consumeContext">The consume context value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public RequestHandle<T> CreateRequest<T>(ConsumeContext consumeContext, T message, RequestTimeout timeout, CancellationToken cancellationToken)
        where T : class
    {
        return _clientFactory.CreateRequest(consumeContext, message, timeout, cancellationToken);
    }

    /// <summary>
    /// Creates request.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="consumeContext">The consume context value.</param>
    /// <param name="destinationAddress">The destination address value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public RequestHandle<T> CreateRequest<T>(ConsumeContext consumeContext, Uri destinationAddress, T message, RequestTimeout timeout, CancellationToken cancellationToken)
        where T : class
    {
        return _clientFactory.CreateRequest(consumeContext, destinationAddress, message, timeout, cancellationToken);
    }

    /// <summary>
    /// Creates request.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="values">The values value.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public RequestHandle<T> CreateRequest<T>(object values, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class
    {
        return _clientFactory.CreateRequest<T>(values, timeout, cancellationToken);
    }

    /// <summary>
    /// Creates request.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="destinationAddress">The destination address value.</param>
    /// <param name="values">The values value.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public RequestHandle<T> CreateRequest<T>(Uri destinationAddress, object values, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class
    {
        return _clientFactory.CreateRequest<T>(destinationAddress, values, timeout, cancellationToken);
    }

    /// <summary>
    /// Creates request.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="consumeContext">The consume context value.</param>
    /// <param name="values">The values value.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public RequestHandle<T> CreateRequest<T>(ConsumeContext consumeContext, object values, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class
    {
        return _clientFactory.CreateRequest<T>(consumeContext, values, timeout, cancellationToken);
    }

    /// <summary>
    /// Creates request.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="consumeContext">The consume context value.</param>
    /// <param name="destinationAddress">The destination address value.</param>
    /// <param name="values">The values value.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public RequestHandle<T> CreateRequest<T>(ConsumeContext consumeContext, Uri destinationAddress, object values,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class
    {
        return _clientFactory.CreateRequest<T>(consumeContext, destinationAddress, values, timeout, cancellationToken);
    }

    /// <summary>
    /// Creates request client.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="timeout">The timeout value.</param>
    /// <returns>The result of the operation.</returns>
    public IRequestClient<T> CreateRequestClient<T>(RequestTimeout timeout)
        where T : class
    {
        return _clientFactory.CreateRequestClient<T>(timeout);
    }

    /// <summary>
    /// Creates request client.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="consumeContext">The consume context value.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <returns>The result of the operation.</returns>
    public IRequestClient<T> CreateRequestClient<T>(ConsumeContext? consumeContext, RequestTimeout timeout)
        where T : class
    {
        return _clientFactory.CreateRequestClient<T>(consumeContext, timeout);
    }

    /// <summary>
    /// Creates request client.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="destinationAddress">The destination address value.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <returns>The result of the operation.</returns>
    public IRequestClient<T> CreateRequestClient<T>(Uri destinationAddress, RequestTimeout timeout)
        where T : class
    {
        return _clientFactory.CreateRequestClient<T>(destinationAddress, timeout);
    }

    /// <summary>
    /// Creates request client.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="consumeContext">The consume context value.</param>
    /// <param name="destinationAddress">The destination address value.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <returns>The result of the operation.</returns>
    public IRequestClient<T> CreateRequestClient<T>(ConsumeContext? consumeContext, Uri destinationAddress, RequestTimeout timeout)
        where T : class
    {
        return _clientFactory.CreateRequestClient<T>(consumeContext, destinationAddress, timeout);
    }

    /// <summary>
    /// Gets the context value.
    /// </summary>
    public ClientFactoryContext Context => _clientFactory.Context;

    /// <summary>
    /// Connects consume pipe.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="pipe">The pipe value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe)
        where T : class
    {
        return _dispatcher.ConnectConsumePipe(pipe);
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
        return _dispatcher.ConnectConsumePipe(pipe, options);
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
        return _dispatcher.ConnectRequestPipe(requestId, pipe);
    }

    /// <summary>
    /// Connects consume observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectConsumeObserver(IConsumeObserver observer)
    {
        return new MultipleConnectHandle(_dispatcher.ConnectConsumeObserver(observer), _responseDispatcher.ConnectConsumeObserver(observer));
    }

    /// <summary>
    /// Connects consume message observer.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectConsumeMessageObserver<T>(IConsumeMessageObserver<T> observer)
        where T : class
    {
        return new MultipleConnectHandle(_dispatcher.ConnectConsumeMessageObserver(observer), _responseDispatcher.ConnectConsumeMessageObserver(observer));
    }

    Task PublishInternalAsync<T>(CancellationToken cancellationToken, T message, IPipe<PublishContext<T>>? pipe = default)
        where T : class
    {
        Task<ISendEndpoint> sendEndpointTask = GetPublishSendEndpointAsync<T>(cancellationToken: cancellationToken);
        if (sendEndpointTask.Status == TaskStatus.RanToCompletion)
        {
            var sendEndpoint = sendEndpointTask.Result;

            return pipe != null && pipe.IsNotEmpty()
                ? sendEndpoint.SendAsync(message, new PublishSendPipeAdapter<T>(pipe), cancellationToken)
                : sendEndpoint.SendAsync(message, cancellationToken);
        }

        async Task PublishAsync()
        {
            var sendEndpoint = await sendEndpointTask.ConfigureAwait(false);

            if (pipe != null && pipe.IsNotEmpty())
                await sendEndpoint.SendAsync(message, new PublishSendPipeAdapter<T>(pipe), cancellationToken).ConfigureAwait(false);
            else
                await sendEndpoint.SendAsync(message, cancellationToken).ConfigureAwait(false);
        }

        return PublishAsync();
    }

    Task PublishInternalAsync<T>(CancellationToken cancellationToken, object values, IPipe<PublishContext<T>>? pipe = default)
        where T : class
    {
        Task<ISendEndpoint> sendEndpointTask = GetPublishSendEndpointAsync<T>(cancellationToken: cancellationToken);
        if (sendEndpointTask.Status == TaskStatus.RanToCompletion)
        {
            var sendEndpoint = sendEndpointTask.Result;

            return pipe != null && pipe.IsNotEmpty()
                ? sendEndpoint.SendAsync(values, new PublishSendPipeAdapter<T>(pipe), cancellationToken)
                : sendEndpoint.SendAsync<T>(values, cancellationToken);
        }

        async Task PublishAsync()
        {
            var sendEndpoint = await sendEndpointTask.ConfigureAwait(false);

            if (pipe != null && pipe.IsNotEmpty())
                await sendEndpoint.SendAsync(values, new PublishSendPipeAdapter<T>(pipe), cancellationToken).ConfigureAwait(false);
            else
                await sendEndpoint.SendAsync<T>(values, cancellationToken).ConfigureAwait(false);
        }

        return PublishAsync();
    }
}
