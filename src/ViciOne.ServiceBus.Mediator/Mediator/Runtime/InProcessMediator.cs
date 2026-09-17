using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Clients;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Internals.Dispatching;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Mediator.Contexts;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Mediator.Runtime;

/// <summary>Dispatches messages directly through in-process receive pipelines without a transport broker.</summary>
internal sealed class InProcessMediator :
    IMediator,
    Advanced.IAdvancedSendEndpoint,
    Advanced.IAdvancedPublishEndpoint,
    IAsyncDisposable
{
    readonly ClientFactory _clientFactory;
    readonly ConnectHandle _configuredConsumeObservers;
    readonly object _disposeLock = new();
    readonly IReceivePipeDispatcher _dispatcher;
    readonly MediatorSendEndpoint _endpoint;
    readonly IReceivePipeDispatcher _responseDispatcher;
    Task? _disposeTask;

    /// <summary>Initializes dispatch, response routing, request deadlines, and message limits.</summary>
    /// <param name="logContext">The log context inherited by mediator operations.</param>
    /// <param name="configuration">The primary mediator endpoint configuration.</param>
    /// <param name="dispatcher">The primary message dispatcher.</param>
    /// <param name="responseConfiguration">The request-response endpoint configuration.</param>
    /// <param name="responseDispatcher">The response message dispatcher.</param>
    /// <param name="consumeObservers">The consume observers declared during configuration.</param>
    /// <param name="sendObservers">The send observers declared during configuration or connected at runtime.</param>
    /// <param name="publishObservers">The publish observers declared during configuration or connected at runtime.</param>
    /// <param name="limits">The enforced mediator message limits.</param>
    /// <param name="timeProvider">The time source used for request deadlines.</param>
    public InProcessMediator(
        ILogContext? logContext,
        IReceiveEndpointConfiguration configuration,
        IReceivePipeDispatcher dispatcher,
        IReceiveEndpointConfiguration responseConfiguration,
        IReceivePipeDispatcher responseDispatcher,
        ConsumeObservable consumeObservers,
        SendObservable sendObservers,
        PublishObservable publishObservers,
        MessageLimits limits,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(responseConfiguration);
        ArgumentNullException.ThrowIfNull(responseDispatcher);
        ArgumentNullException.ThrowIfNull(consumeObservers);
        ArgumentNullException.ThrowIfNull(sendObservers);
        ArgumentNullException.ThrowIfNull(publishObservers);
        ArgumentNullException.ThrowIfNull(limits);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _responseDispatcher = responseDispatcher;
        _dispatcher = dispatcher;
        _endpoint = new MediatorSendEndpoint(
            configuration,
            dispatcher,
            logContext,
            sendObservers,
            publishObservers,
            responseConfiguration,
            responseDispatcher,
            limits);

        _configuredConsumeObservers = ConnectConfiguredObservers(dispatcher, responseDispatcher, consumeObservers);

        var clientFactoryContext = new MediatorClientFactoryContext(
            _endpoint,
            responseConfiguration.ConsumePipe,
            responseConfiguration.InputAddress,
            timeProvider: timeProvider);
        _clientFactory = new ClientFactory(clientFactoryContext);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        lock (_disposeLock)
            return new ValueTask(_disposeTask ??= DisposeCoreAsync());
    }

    async Task DisposeCoreAsync()
    {
        Task clientFactoryCleanup = _clientFactory.DisposeAsync().AsTask();
        Task observerCleanup = ((IAsyncDisposable)_configuredConsumeObservers).DisposeAsync().AsTask();
        Task cleanup = Task.WhenAll(clientFactoryCleanup, observerCleanup);

        try
        {
            await cleanup.ConfigureAwait(false);
        }
        catch
        {
            var failures = new List<Exception>();
            AddFailures(clientFactoryCleanup, failures);
            AddFailures(observerCleanup, failures);

            if (failures.Count > 1)
                throw new AggregateException("Mediator cleanup failed.", failures);
            if (failures.Count == 1)
                ExceptionDispatchInfo.Capture(failures[0]).Throw();

            throw;
        }
    }

    static void AddFailures(Task cleanup, ICollection<Exception> failures)
    {
        if (cleanup.Exception is not { } aggregate)
            return;

        foreach (Exception failure in aggregate.Flatten().InnerExceptions)
            failures.Add(failure);
    }

    /// <inheritdoc />
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        return _endpoint.ConnectSendObserver(observer);
    }

    /// <inheritdoc />
    public Task SendAsync<T>(T message, CancellationToken cancellationToken)
        where T : class
    {
        return _endpoint.SendAsync(message, cancellationToken);
    }

    /// <inheritdoc />
    public Task SendAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        return _endpoint.SendAsync(message, pipe, cancellationToken);
    }

    /// <inheritdoc />
    public Task SendAsync<T>(T message, IPipe<SendContext> pipe, CancellationToken cancellationToken)
        where T : class
    {
        return _endpoint.SendAsync(message, pipe, cancellationToken);
    }

    /// <inheritdoc />
    public Task SendAsync(object message, CancellationToken cancellationToken)
    {
        return _endpoint.SendAsync(message, cancellationToken);
    }

    /// <inheritdoc />
    public Task SendAsync(object message, Type messageType, CancellationToken cancellationToken)
    {
        return _endpoint.SendAsync(message, messageType, cancellationToken);
    }

    /// <inheritdoc />
    public Task SendAsync(object message, IPipe<SendContext> pipe, CancellationToken cancellationToken)
    {
        return _endpoint.SendAsync(message, pipe, cancellationToken);
    }

    /// <inheritdoc />
    public Task SendAsync(object message, Type messageType, IPipe<SendContext> pipe, CancellationToken cancellationToken)
    {
        return _endpoint.SendAsync(message, messageType, pipe, cancellationToken);
    }

    /// <inheritdoc />
    public Task SendAsync<T>(object values, CancellationToken cancellationToken)
        where T : class
    {
        return _endpoint.SendAsync<T>(values, cancellationToken);
    }

    /// <inheritdoc />
    public Task SendAsync<T>(object values, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        return _endpoint.SendAsync(values, pipe, cancellationToken);
    }

    /// <inheritdoc />
    public Task SendAsync<T>(object values, IPipe<SendContext> pipe, CancellationToken cancellationToken)
        where T : class
    {
        return _endpoint.SendAsync<T>(values, pipe, cancellationToken);
    }

    /// <inheritdoc />
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        return _endpoint.ConnectPublishObserver(observer);
    }

    /// <inheritdoc />
    public Task<ISendEndpoint> GetPublishSendEndpointAsync<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        return _endpoint.GetPublishSendEndpointAsync<T>(cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public Task PublishAsync<T>(T message, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        return PublishInternalAsync(cancellationToken, message);
    }

    /// <inheritdoc />
    public Task PublishAsync<T>(T message, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(publishPipe);
        return PublishInternalAsync(cancellationToken, message, publishPipe);
    }

    /// <inheritdoc />
    public Task PublishAsync<T>(T message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(publishPipe);
        return PublishInternalAsync(cancellationToken, message, publishPipe);
    }

    /// <inheritdoc />
    public Task PublishAsync(object message, CancellationToken cancellationToken)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var messageType = message.GetType();

        return PublishEndpointDispatcher.PublishAsync(this, message, messageType, cancellationToken);
    }

    /// <inheritdoc />
    public Task PublishAsync(object message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (publishPipe == null)
            throw new ArgumentNullException(nameof(publishPipe));

        var messageType = message.GetType();

        return PublishEndpointDispatcher.PublishAsync(this, message, messageType, publishPipe, cancellationToken);
    }

    /// <inheritdoc />
    public Task PublishAsync(object message, Type messageType, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);
        return PublishEndpointDispatcher.PublishAsync(this, message, messageType, cancellationToken);
    }

    /// <inheritdoc />
    public Task PublishAsync(object message, Type messageType, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentNullException.ThrowIfNull(publishPipe);
        return PublishEndpointDispatcher.PublishAsync(this, message, messageType, publishPipe, cancellationToken);
    }

    /// <inheritdoc />
    public Task PublishAsync<T>(object values, CancellationToken cancellationToken)
        where T : class
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        return PublishInternalAsync<T>(cancellationToken, values);
    }

    /// <inheritdoc />
    public Task PublishAsync<T>(object values, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken)
        where T : class
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));
        if (publishPipe == null)
            throw new ArgumentNullException(nameof(publishPipe));

        return PublishInternalAsync(cancellationToken, values, publishPipe);
    }

    /// <inheritdoc />
    public Task PublishAsync<T>(object values, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken)
        where T : class
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));
        if (publishPipe == null)
            throw new ArgumentNullException(nameof(publishPipe));

        return PublishInternalAsync<T>(cancellationToken, values, publishPipe);
    }

    /// <inheritdoc />
    public RequestHandle<T> CreateRequest<T>(T message, RequestTimeout timeout, CancellationToken cancellationToken)
        where T : class
    {
        return _clientFactory.CreateRequest(message, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public RequestHandle<T> CreateRequest<T>(Uri destinationAddress, T message, RequestTimeout timeout, CancellationToken cancellationToken)
        where T : class
    {
        return _clientFactory.CreateRequest(destinationAddress, message, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public RequestHandle<T> CreateRequest<T>(ConsumeContext consumeContext, T message, RequestTimeout timeout, CancellationToken cancellationToken)
        where T : class
    {
        return _clientFactory.CreateRequest(consumeContext, message, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public RequestHandle<T> CreateRequest<T>(ConsumeContext consumeContext, Uri destinationAddress, T message, RequestTimeout timeout, CancellationToken cancellationToken)
        where T : class
    {
        return _clientFactory.CreateRequest(consumeContext, destinationAddress, message, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public RequestHandle<T> CreateRequest<T>(object values, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class
    {
        return _clientFactory.CreateRequest<T>(values, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public RequestHandle<T> CreateRequest<T>(Uri destinationAddress, object values, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class
    {
        return _clientFactory.CreateRequest<T>(destinationAddress, values, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public RequestHandle<T> CreateRequest<T>(ConsumeContext consumeContext, object values, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class
    {
        return _clientFactory.CreateRequest<T>(consumeContext, values, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public RequestHandle<T> CreateRequest<T>(ConsumeContext consumeContext, Uri destinationAddress, object values,
        RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class
    {
        return _clientFactory.CreateRequest<T>(consumeContext, destinationAddress, values, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public IRequestClient<T> CreateRequestClient<T>(RequestTimeout timeout)
        where T : class
    {
        return _clientFactory.CreateRequestClient<T>(timeout);
    }

    /// <inheritdoc />
    public IRequestClient<T> CreateRequestClient<T>(ConsumeContext consumeContext, RequestTimeout timeout)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(consumeContext);
        return _clientFactory.CreateRequestClient<T>(consumeContext, timeout);
    }

    /// <inheritdoc />
    public IRequestClient<T> CreateRequestClient<T>(Uri destinationAddress, RequestTimeout timeout)
        where T : class
    {
        return _clientFactory.CreateRequestClient<T>(destinationAddress, timeout);
    }

    /// <inheritdoc />
    public IRequestClient<T> CreateRequestClient<T>(ConsumeContext consumeContext, Uri destinationAddress, RequestTimeout timeout)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(consumeContext);
        ArgumentNullException.ThrowIfNull(destinationAddress);
        return _clientFactory.CreateRequestClient<T>(consumeContext, destinationAddress, timeout);
    }

    /// <inheritdoc />
    public ClientFactoryContext Context => _clientFactory.Context;

    /// <inheritdoc />
    public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe)
        where T : class
    {
        return _dispatcher.ConnectConsumePipe(pipe);
    }

    /// <inheritdoc />
    public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options)
        where T : class
    {
        return _dispatcher.ConnectConsumePipe(pipe, options);
    }

    /// <inheritdoc />
    public ConnectHandle ConnectRequestPipe<T>(Guid requestId, IPipe<ConsumeContext<T>> pipe)
        where T : class
    {
        return _dispatcher.ConnectRequestPipe(requestId, pipe);
    }

    /// <inheritdoc />
    public ConnectHandle ConnectConsumeObserver(IConsumeObserver observer)
    {
        return new MultipleConnectHandle(_dispatcher.ConnectConsumeObserver(observer), _responseDispatcher.ConnectConsumeObserver(observer));
    }

    /// <inheritdoc />
    public ConnectHandle ConnectConsumeMessageObserver<T>(IConsumeMessageObserver<T> observer)
        where T : class
    {
        return new MultipleConnectHandle(_dispatcher.ConnectConsumeMessageObserver(observer), _responseDispatcher.ConnectConsumeMessageObserver(observer));
    }

    async Task PublishInternalAsync<T>(CancellationToken cancellationToken, T message, IPipe<PublishContext<T>>? pipe = default)
        where T : class
    {
        ISendEndpoint sendEndpoint = await GetPublishSendEndpointAsync<T>(cancellationToken: cancellationToken).ConfigureAwait(false);
        if (pipe != null && pipe.IsNotEmpty())
            await sendEndpoint.SendAsync(message, new PublishSendPipeAdapter<T>(pipe), cancellationToken).ConfigureAwait(false);
        else
            await sendEndpoint.SendAsync(message, cancellationToken).ConfigureAwait(false);
    }

    async Task PublishInternalAsync<T>(CancellationToken cancellationToken, object values, IPipe<PublishContext<T>>? pipe = default)
        where T : class
    {
        ISendEndpoint sendEndpoint = await GetPublishSendEndpointAsync<T>(cancellationToken: cancellationToken).ConfigureAwait(false);
        if (pipe != null && pipe.IsNotEmpty())
            await sendEndpoint.SendAsync(values, new PublishSendPipeAdapter<T>(pipe), cancellationToken).ConfigureAwait(false);
        else
            await sendEndpoint.SendAsync<T>(values, cancellationToken).ConfigureAwait(false);
    }

    static ConnectHandle ConnectConfiguredObservers(
        IReceivePipeDispatcher dispatcher,
        IReceivePipeDispatcher responseDispatcher,
        IConsumeObserver consumeObservers)
    {
        ConnectHandle primary = dispatcher.ConnectConsumeObserver(consumeObservers);
        try
        {
            ConnectHandle response = responseDispatcher.ConnectConsumeObserver(consumeObservers);
            return new MultipleConnectHandle(primary, response);
        }
        catch
        {
            primary.Dispose();
            throw;
        }
    }
}
