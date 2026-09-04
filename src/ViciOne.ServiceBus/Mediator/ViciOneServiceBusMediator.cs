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

    public ViciOneServiceBusMediator(ILogContext logContext, IReceiveEndpointConfiguration configuration, IReceivePipeDispatcher dispatcher,
        IReceiveEndpointConfiguration responseConfiguration, IReceivePipeDispatcher responseDispatcher)
        : this(logContext, configuration, dispatcher, responseConfiguration, responseDispatcher, TimeProvider.System)
    {
    }

    public ViciOneServiceBusMediator(
        ILogContext? logContext,
        IReceiveEndpointConfiguration configuration,
        IReceivePipeDispatcher dispatcher,
        IReceiveEndpointConfiguration responseConfiguration,
        IReceivePipeDispatcher responseDispatcher,
        TimeProvider timeProvider)
    {
        if (timeProvider == null)
            throw new ArgumentNullException(nameof(timeProvider));

        _responseDispatcher = responseDispatcher;
        _dispatcher = dispatcher;
        var sendObservable = new SendObservable();

        _endpoint = new MediatorSendEndpoint(configuration, dispatcher, logContext, sendObservable, responseConfiguration, responseDispatcher);

        var clientFactoryContext = new MediatorClientFactoryContext(
            _endpoint,
            responseConfiguration.ConsumePipe,
            responseConfiguration.InputAddress,
            timeProvider: timeProvider);
        _clientFactory = new ClientFactory(clientFactoryContext);
    }

    public ValueTask DisposeAsync()
    {
        return _clientFactory.DisposeAsync();
    }

    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        return _endpoint.ConnectSendObserver(observer);
    }

    public Task SendAsync<T>(T message, CancellationToken cancellationToken)
        where T : class
    {
        return _endpoint.SendAsync(message, cancellationToken);
    }

    public Task SendAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        return _endpoint.SendAsync(message, pipe, cancellationToken);
    }

    public Task SendAsync<T>(T message, IPipe<SendContext> pipe, CancellationToken cancellationToken)
        where T : class
    {
        return _endpoint.SendAsync(message, pipe, cancellationToken);
    }

    public Task SendAsync(object message, CancellationToken cancellationToken)
    {
        return _endpoint.SendAsync(message, cancellationToken);
    }

    public Task SendAsync(object message, Type messageType, CancellationToken cancellationToken)
    {
        return _endpoint.SendAsync(message, messageType, cancellationToken);
    }

    public Task SendAsync(object message, IPipe<SendContext> pipe, CancellationToken cancellationToken)
    {
        return _endpoint.SendAsync(message, pipe, cancellationToken);
    }

    public Task SendAsync(object message, Type messageType, IPipe<SendContext> pipe, CancellationToken cancellationToken)
    {
        return _endpoint.SendAsync(message, messageType, pipe, cancellationToken);
    }

    public Task SendAsync<T>(object values, CancellationToken cancellationToken)
        where T : class
    {
        return _endpoint.SendAsync<T>(values, cancellationToken);
    }

    public Task SendAsync<T>(object values, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        return _endpoint.SendAsync(values, pipe, cancellationToken);
    }

    public Task SendAsync<T>(object values, IPipe<SendContext> pipe, CancellationToken cancellationToken)
        where T : class
    {
        return _endpoint.SendAsync<T>(values, pipe, cancellationToken);
    }

    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        return _endpoint.ConnectPublishObserver(observer);
    }

    public Task<ISendEndpoint> GetPublishSendEndpointAsync<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        return _endpoint.GetPublishSendEndpointAsync<T>(cancellationToken: cancellationToken);
    }

    public Task PublishAsync<T>(T message, CancellationToken cancellationToken)
        where T : class
    {
        return PublishInternalAsync(cancellationToken, message);
    }

    public Task PublishAsync<T>(T message, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken)
        where T : class
    {
        return PublishInternalAsync(cancellationToken, message, publishPipe);
    }

    public Task PublishAsync<T>(T message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken)
        where T : class
    {
        return PublishInternalAsync(cancellationToken, message, publishPipe);
    }

    public Task PublishAsync(object message, CancellationToken cancellationToken)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var messageType = message.GetType();

        return PublishEndpointConverterCache.PublishAsync(this, message, messageType, cancellationToken);
    }

    public Task PublishAsync(object message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var messageType = message.GetType();

        return PublishEndpointConverterCache.PublishAsync(this, message, messageType, publishPipe, cancellationToken);
    }

    public Task PublishAsync(object message, Type messageType, CancellationToken cancellationToken)
    {
        return PublishEndpointConverterCache.PublishAsync(this, message, messageType, cancellationToken);
    }

    public Task PublishAsync(object message, Type messageType, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken)
    {
        return PublishEndpointConverterCache.PublishAsync(this, message, messageType, publishPipe, cancellationToken);
    }

    public Task PublishAsync<T>(object values, CancellationToken cancellationToken)
        where T : class
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        return PublishInternalAsync<T>(cancellationToken, values);
    }

    public Task PublishAsync<T>(object values, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken)
        where T : class
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        return PublishInternalAsync(cancellationToken, values, publishPipe);
    }

    public Task PublishAsync<T>(object values, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken)
        where T : class
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        return PublishInternalAsync<T>(cancellationToken, values, publishPipe);
    }

    public RequestHandle<T> CreateRequest<T>(T message, CancellationToken cancellationToken, RequestTimeout timeout)
        where T : class
    {
        return _clientFactory.CreateRequest(message, cancellationToken, timeout);
    }

    public RequestHandle<T> CreateRequest<T>(Uri destinationAddress, T message, CancellationToken cancellationToken, RequestTimeout timeout)
        where T : class
    {
        return _clientFactory.CreateRequest(destinationAddress, message, cancellationToken, timeout);
    }

    public RequestHandle<T> CreateRequest<T>(ConsumeContext consumeContext, T message, CancellationToken cancellationToken, RequestTimeout timeout)
        where T : class
    {
        return _clientFactory.CreateRequest(consumeContext, message, cancellationToken, timeout);
    }

    public RequestHandle<T> CreateRequest<T>(ConsumeContext consumeContext, Uri destinationAddress, T message, CancellationToken cancellationToken,
        RequestTimeout timeout)
        where T : class
    {
        return _clientFactory.CreateRequest(consumeContext, destinationAddress, message, cancellationToken, timeout);
    }

    public RequestHandle<T> CreateRequest<T>(object values, CancellationToken cancellationToken = default, RequestTimeout timeout = default)
        where T : class
    {
        return _clientFactory.CreateRequest<T>(values, cancellationToken, timeout);
    }

    public RequestHandle<T> CreateRequest<T>(Uri destinationAddress, object values, CancellationToken cancellationToken = default,
        RequestTimeout timeout = default)
        where T : class
    {
        return _clientFactory.CreateRequest<T>(destinationAddress, values, cancellationToken, timeout);
    }

    public RequestHandle<T> CreateRequest<T>(ConsumeContext consumeContext, object values, CancellationToken cancellationToken = default,
        RequestTimeout timeout = default)
        where T : class
    {
        return _clientFactory.CreateRequest<T>(consumeContext, values, cancellationToken, timeout);
    }

    public RequestHandle<T> CreateRequest<T>(ConsumeContext consumeContext, Uri destinationAddress, object values,
        CancellationToken cancellationToken = default,
        RequestTimeout timeout = default)
        where T : class
    {
        return _clientFactory.CreateRequest<T>(consumeContext, destinationAddress, values, cancellationToken, timeout);
    }

    public IRequestClient<T> CreateRequestClient<T>(RequestTimeout timeout)
        where T : class
    {
        return _clientFactory.CreateRequestClient<T>(timeout);
    }

    public IRequestClient<T> CreateRequestClient<T>(ConsumeContext? consumeContext, RequestTimeout timeout)
        where T : class
    {
        return _clientFactory.CreateRequestClient<T>(consumeContext, timeout);
    }

    public IRequestClient<T> CreateRequestClient<T>(Uri destinationAddress, RequestTimeout timeout)
        where T : class
    {
        return _clientFactory.CreateRequestClient<T>(destinationAddress, timeout);
    }

    public IRequestClient<T> CreateRequestClient<T>(ConsumeContext? consumeContext, Uri destinationAddress, RequestTimeout timeout)
        where T : class
    {
        return _clientFactory.CreateRequestClient<T>(consumeContext, destinationAddress, timeout);
    }

    public ClientFactoryContext Context => _clientFactory.Context;

    public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe)
        where T : class
    {
        return _dispatcher.ConnectConsumePipe(pipe);
    }

    public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options)
        where T : class
    {
        return _dispatcher.ConnectConsumePipe(pipe, options);
    }

    public ConnectHandle ConnectRequestPipe<T>(Guid requestId, IPipe<ConsumeContext<T>> pipe)
        where T : class
    {
        return _dispatcher.ConnectRequestPipe(requestId, pipe);
    }

    public ConnectHandle ConnectConsumeObserver(IConsumeObserver observer)
    {
        return new MultipleConnectHandle(_dispatcher.ConnectConsumeObserver(observer), _responseDispatcher.ConnectConsumeObserver(observer));
    }

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
