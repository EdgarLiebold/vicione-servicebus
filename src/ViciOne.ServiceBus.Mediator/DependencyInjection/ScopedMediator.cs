using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Clients;
using ViciOne.ServiceBus.Context;
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
    ClientFactory? _clientFactory;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="mediator">The mediator.</param>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    public ScopedMediator(IMediator mediator, IServiceProvider provider)
        : base(mediator)
    {
        _mediator = mediator;
        _provider = provider;
    }

    ClientFactory ClientFactory => _clientFactory ??= new ClientFactory(new ScopedClientFactoryContext(_mediator, _provider));

    /// <summary>Releases request clients created for this scope without disposing the singleton mediator.</summary>
    /// <returns>A task that represents scoped client cleanup.</returns>
    public ValueTask DisposeAsync()
    {
        return _clientFactory?.DisposeAsync() ?? default;
    }

    /// <summary>Connects publish observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        return _mediator.ConnectPublishObserver(observer);
    }

    /// <summary>Gets publish send endpoint.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public async Task<ISendEndpoint> GetPublishSendEndpointAsync<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        var endpoint = await _mediator.GetPublishSendEndpointAsync<T>(cancellationToken: cancellationToken).ConfigureAwait(false);
        return new ScopedSendEndpoint(endpoint, _provider);
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class
    {
        return PublishInternalAsync(cancellationToken, message);
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="publishPipe">The publish pipe.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishAsync<T>(T message, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default)
        where T : class
    {
        return PublishInternalAsync(cancellationToken, message, publishPipe);
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="publishPipe">The publish pipe.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishAsync<T>(T message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
        where T : class
    {
        return PublishInternalAsync(cancellationToken, message, publishPipe);
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishAsync(object message, CancellationToken cancellationToken = default)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var messageType = message.GetType();
        return PublishEndpointConverterCache.PublishAsync(this, message, messageType, cancellationToken);
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="publishPipe">The publish pipe.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishAsync(object message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var messageType = message.GetType();
        return PublishEndpointConverterCache.PublishAsync(this, message, messageType, publishPipe, cancellationToken);
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishAsync(object message, Type messageType, CancellationToken cancellationToken = default)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        return PublishEndpointConverterCache.PublishAsync(this, message, messageType, cancellationToken);
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="publishPipe">The publish pipe.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishAsync(object message, Type messageType, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        return PublishEndpointConverterCache.PublishAsync(this, message, messageType, publishPipe, cancellationToken);
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="values">The values.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishAsync<T>(object values, CancellationToken cancellationToken = default)
        where T : class
    {
        return PublishInternalAsync<T>(cancellationToken, values);
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="values">The values.</param>
    /// <param name="publishPipe">The publish pipe.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishAsync<T>(object values, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default)
        where T : class
    {
        return PublishInternalAsync(cancellationToken, values, publishPipe);
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="values">The values.</param>
    /// <param name="publishPipe">The publish pipe.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishAsync<T>(object values, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
        where T : class
    {
        return PublishInternalAsync<T>(cancellationToken, values, publishPipe);
    }

    /// <summary>Gets the context.</summary>
    public ClientFactoryContext Context => ClientFactory.Context;

    /// <summary>Creates request.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The created request.</returns>
    public RequestHandle<T> CreateRequest<T>(T message, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class
    {
        return ClientFactory.CreateRequest(message, timeout, cancellationToken);
    }

    /// <summary>Creates request.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The created request.</returns>
    public RequestHandle<T> CreateRequest<T>(Uri destinationAddress, T message, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class
    {
        return ClientFactory.CreateRequest(destinationAddress, message, timeout, cancellationToken);
    }

    /// <summary>Creates request.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="consumeContext">The consume context.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The created request.</returns>
    public RequestHandle<T> CreateRequest<T>(ConsumeContext consumeContext, T message, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class
    {
        return ClientFactory.CreateRequest(consumeContext, message, timeout, cancellationToken);
    }

    /// <summary>Creates request.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="consumeContext">The consume context.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The created request.</returns>
    public RequestHandle<T> CreateRequest<T>(ConsumeContext consumeContext, Uri destinationAddress, T message,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class
    {
        return ClientFactory.CreateRequest(consumeContext, destinationAddress, message, timeout, cancellationToken);
    }

    /// <summary>Creates request.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="values">The values.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The created request.</returns>
    public RequestHandle<T> CreateRequest<T>(object values, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class
    {
        return ClientFactory.CreateRequest<T>(values, timeout, cancellationToken);
    }

    /// <summary>Creates request.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="values">The values.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The created request.</returns>
    public RequestHandle<T> CreateRequest<T>(Uri destinationAddress, object values, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class
    {
        return ClientFactory.CreateRequest<T>(destinationAddress, values, timeout, cancellationToken);
    }

    /// <summary>Creates request.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="consumeContext">The consume context.</param>
    /// <param name="values">The values.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The created request.</returns>
    public RequestHandle<T> CreateRequest<T>(ConsumeContext consumeContext, object values, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class
    {
        return ClientFactory.CreateRequest<T>(consumeContext, values, timeout, cancellationToken);
    }

    /// <summary>Creates request.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="consumeContext">The consume context.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="values">The values.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The created request.</returns>
    public RequestHandle<T> CreateRequest<T>(ConsumeContext consumeContext, Uri destinationAddress, object values,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class
    {
        return ClientFactory.CreateRequest<T>(consumeContext, destinationAddress, values, timeout, cancellationToken);
    }

    /// <summary>Creates request client.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <returns>The created request client.</returns>
    public IRequestClient<T> CreateRequestClient<T>(RequestTimeout timeout = default)
        where T : class
    {
        return ClientFactory.CreateRequestClient<T>(timeout);
    }

    /// <summary>Creates request client.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="consumeContext">The consume context.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <returns>The created request client.</returns>
    public IRequestClient<T> CreateRequestClient<T>(ConsumeContext consumeContext, RequestTimeout timeout = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(consumeContext);
        return ClientFactory.CreateRequestClient<T>(consumeContext, timeout);
    }

    /// <summary>Creates request client.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <returns>The created request client.</returns>
    public IRequestClient<T> CreateRequestClient<T>(Uri destinationAddress, RequestTimeout timeout = default)
        where T : class
    {
        return ClientFactory.CreateRequestClient<T>(destinationAddress, timeout);
    }

    /// <summary>Creates request client.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="consumeContext">The consume context.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <returns>The created request client.</returns>
    public IRequestClient<T> CreateRequestClient<T>(ConsumeContext consumeContext, Uri destinationAddress, RequestTimeout timeout = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(consumeContext);
        ArgumentNullException.ThrowIfNull(destinationAddress);
        return ClientFactory.CreateRequestClient<T>(consumeContext, destinationAddress, timeout);
    }

    /// <summary>Connects consume pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe)
        where T : class
    {
        return _mediator.ConnectConsumePipe(pipe);
    }

    /// <summary>Connects consume pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options)
        where T : class
    {
        return _mediator.ConnectConsumePipe(pipe, options);
    }

    /// <summary>Connects request pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="requestId">The request id.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectRequestPipe<T>(Guid requestId, IPipe<ConsumeContext<T>> pipe)
        where T : class
    {
        return _mediator.ConnectRequestPipe(requestId, pipe);
    }

    /// <summary>Connects consume observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumeObserver(IConsumeObserver observer)
    {
        return _mediator.ConnectConsumeObserver(observer);
    }

    /// <summary>Connects consume message observer.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumeMessageObserver<T>(IConsumeMessageObserver<T> observer)
        where T : class
    {
        return _mediator.ConnectConsumeMessageObserver(observer);
    }

    /// <summary>Gets pipe proxy.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <returns>The pipe proxy.</returns>
    protected override IPipe<SendContext<T>> GetPipeProxy<T>(IPipe<SendContext<T>>? pipe = default)
    {
        return new ScopedSendPipeAdapter<T>(_provider, pipe);
    }

    Task PublishInternalAsync<T>(CancellationToken cancellationToken, T message, IPipe<PublishContext<T>>? pipe = null)
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

    Task PublishInternalAsync<T>(CancellationToken cancellationToken, object values, IPipe<PublishContext<T>>? pipe = null)
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
