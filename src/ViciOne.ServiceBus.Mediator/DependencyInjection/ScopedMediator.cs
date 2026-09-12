using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Clients;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Internals.Dispatching;
using ViciOne.ServiceBus.Mediator;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Preserves the current dependency-injection scope across in-process dispatch operations.</summary>
internal sealed class ScopedMediator :
    SendEndpointProxy,
    IScopedMediator,
    Advanced.IAdvancedPublishEndpoint
{
    readonly IMediator _mediator;
    readonly IServiceProvider _provider;
    readonly Lazy<ClientFactory> _clientFactory;

    /// <summary>Creates a scope-preserving view over the singleton mediator.</summary>
    /// <param name="mediator">The singleton mediator that owns the in-process dispatch pipelines.</param>
    /// <param name="provider">The dependency-injection scope used to resolve handlers and filters.</param>
    public ScopedMediator(IMediator mediator, IServiceProvider provider)
        : base(mediator)
    {
        _mediator = mediator;
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _clientFactory = new Lazy<ClientFactory>(
            () => new ClientFactory(new ScopedClientFactoryContext(_mediator, _provider)),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    ClientFactory ClientFactory => _clientFactory.Value;

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        return _clientFactory.IsValueCreated ? _clientFactory.Value.DisposeAsync() : default;
    }

    /// <inheritdoc />
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        return _mediator.ConnectPublishObserver(observer);
    }

    /// <inheritdoc />
    public async Task<ISendEndpoint> GetPublishSendEndpointAsync<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        var endpoint = await _mediator.GetPublishSendEndpointAsync<T>(cancellationToken: cancellationToken).ConfigureAwait(false);
        return new ScopedSendEndpoint(endpoint, _provider);
    }

    /// <inheritdoc />
    public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        return PublishInternalAsync(cancellationToken, message);
    }

    /// <inheritdoc />
    public Task PublishAsync<T>(T message, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(publishPipe);
        return PublishInternalAsync(cancellationToken, message, publishPipe);
    }

    /// <inheritdoc />
    public Task PublishAsync<T>(T message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(publishPipe);
        return PublishInternalAsync(cancellationToken, message, publishPipe);
    }

    /// <inheritdoc />
    public Task PublishAsync(object message, CancellationToken cancellationToken = default)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var messageType = message.GetType();
        return PublishEndpointDispatcher.PublishAsync(this, message, messageType, cancellationToken);
    }

    /// <inheritdoc />
    public Task PublishAsync(object message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (publishPipe == null)
            throw new ArgumentNullException(nameof(publishPipe));

        var messageType = message.GetType();
        return PublishEndpointDispatcher.PublishAsync(this, message, messageType, publishPipe, cancellationToken);
    }

    /// <inheritdoc />
    public Task PublishAsync(object message, Type messageType, CancellationToken cancellationToken = default)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (messageType == null)
            throw new ArgumentNullException(nameof(messageType));

        return PublishEndpointDispatcher.PublishAsync(this, message, messageType, cancellationToken);
    }

    /// <inheritdoc />
    public Task PublishAsync(object message, Type messageType, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (messageType == null)
            throw new ArgumentNullException(nameof(messageType));
        if (publishPipe == null)
            throw new ArgumentNullException(nameof(publishPipe));

        return PublishEndpointDispatcher.PublishAsync(this, message, messageType, publishPipe, cancellationToken);
    }

    /// <inheritdoc />
    public Task PublishAsync<T>(object values, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(values);
        return PublishInternalAsync<T>(cancellationToken, values);
    }

    /// <inheritdoc />
    public Task PublishAsync<T>(object values, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(publishPipe);
        return PublishInternalAsync(cancellationToken, values, publishPipe);
    }

    /// <inheritdoc />
    public Task PublishAsync<T>(object values, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(publishPipe);
        return PublishInternalAsync<T>(cancellationToken, values, publishPipe);
    }

    /// <inheritdoc />
    public ClientFactoryContext Context => ClientFactory.Context;

    /// <inheritdoc />
    public RequestHandle<T> CreateRequest<T>(T message, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class
    {
        return ClientFactory.CreateRequest(message, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public RequestHandle<T> CreateRequest<T>(Uri destinationAddress, T message, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class
    {
        return ClientFactory.CreateRequest(destinationAddress, message, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public RequestHandle<T> CreateRequest<T>(ConsumeContext consumeContext, T message, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class
    {
        return ClientFactory.CreateRequest(consumeContext, message, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public RequestHandle<T> CreateRequest<T>(ConsumeContext consumeContext, Uri destinationAddress, T message,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class
    {
        return ClientFactory.CreateRequest(consumeContext, destinationAddress, message, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public RequestHandle<T> CreateRequest<T>(object values, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class
    {
        return ClientFactory.CreateRequest<T>(values, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public RequestHandle<T> CreateRequest<T>(Uri destinationAddress, object values, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class
    {
        return ClientFactory.CreateRequest<T>(destinationAddress, values, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public RequestHandle<T> CreateRequest<T>(ConsumeContext consumeContext, object values, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class
    {
        return ClientFactory.CreateRequest<T>(consumeContext, values, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public RequestHandle<T> CreateRequest<T>(ConsumeContext consumeContext, Uri destinationAddress, object values,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class
    {
        return ClientFactory.CreateRequest<T>(consumeContext, destinationAddress, values, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public IRequestClient<T> CreateRequestClient<T>(RequestTimeout timeout = default)
        where T : class
    {
        return ClientFactory.CreateRequestClient<T>(timeout);
    }

    /// <inheritdoc />
    public IRequestClient<T> CreateRequestClient<T>(ConsumeContext consumeContext, RequestTimeout timeout = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(consumeContext);
        return ClientFactory.CreateRequestClient<T>(consumeContext, timeout);
    }

    /// <inheritdoc />
    public IRequestClient<T> CreateRequestClient<T>(Uri destinationAddress, RequestTimeout timeout = default)
        where T : class
    {
        return ClientFactory.CreateRequestClient<T>(destinationAddress, timeout);
    }

    /// <inheritdoc />
    public IRequestClient<T> CreateRequestClient<T>(ConsumeContext consumeContext, Uri destinationAddress, RequestTimeout timeout = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(consumeContext);
        ArgumentNullException.ThrowIfNull(destinationAddress);
        return ClientFactory.CreateRequestClient<T>(consumeContext, destinationAddress, timeout);
    }

    /// <inheritdoc />
    public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe)
        where T : class
    {
        return _mediator.ConnectConsumePipe(pipe);
    }

    /// <inheritdoc />
    public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options)
        where T : class
    {
        return _mediator.ConnectConsumePipe(pipe, options);
    }

    /// <inheritdoc />
    public ConnectHandle ConnectRequestPipe<T>(Guid requestId, IPipe<ConsumeContext<T>> pipe)
        where T : class
    {
        return _mediator.ConnectRequestPipe(requestId, pipe);
    }

    /// <inheritdoc />
    public ConnectHandle ConnectConsumeObserver(IConsumeObserver observer)
    {
        return _mediator.ConnectConsumeObserver(observer);
    }

    /// <inheritdoc />
    public ConnectHandle ConnectConsumeMessageObserver<T>(IConsumeMessageObserver<T> observer)
        where T : class
    {
        return _mediator.ConnectConsumeMessageObserver(observer);
    }

    /// <inheritdoc />
    protected override IPipe<SendContext<T>> GetPipeProxy<T>(IPipe<SendContext<T>>? pipe = default)
    {
        return new ScopedSendPipeAdapter<T>(_provider, pipe);
    }

    async Task PublishInternalAsync<T>(CancellationToken cancellationToken, T message, IPipe<PublishContext<T>>? pipe = null)
        where T : class
    {
        ISendEndpoint sendEndpoint = await GetPublishSendEndpointAsync<T>(cancellationToken: cancellationToken).ConfigureAwait(false);
        if (pipe != null && pipe.IsNotEmpty())
            await sendEndpoint.SendAsync(message, new PublishSendPipeAdapter<T>(pipe), cancellationToken).ConfigureAwait(false);
        else
            await sendEndpoint.SendAsync(message, cancellationToken).ConfigureAwait(false);
    }

    async Task PublishInternalAsync<T>(CancellationToken cancellationToken, object values, IPipe<PublishContext<T>>? pipe = null)
        where T : class
    {
        ISendEndpoint sendEndpoint = await GetPublishSendEndpointAsync<T>(cancellationToken: cancellationToken).ConfigureAwait(false);
        if (pipe != null && pipe.IsNotEmpty())
            await sendEndpoint.SendAsync(values, new PublishSendPipeAdapter<T>(pipe), cancellationToken).ConfigureAwait(false);
        else
            await sendEndpoint.SendAsync<T>(values, cancellationToken).ConfigureAwait(false);
    }
}
