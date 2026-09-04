using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Testing.Implementations;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Testing;

/// <summary>
/// A bus text fixture includes a single bus instance with one or more receiving endpoints.
/// </summary>
public abstract class BusTestHarness :
    AsyncTestHarness,
    IBaseTestHarness
{
    bool _busStarted;
    IBusControl? _busControl;
    ISendEndpoint? _busSendEndpoint;
    BusTestConsumeObserver? _consumed;
    ISendEndpoint? _inputQueueSendEndpoint;
    BusTestPublishObserver? _published;
    BusTestReceiveObserver? _received;
    BusTestSendObserver? _sent;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    protected BusTestHarness()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="timeProvider">The time provider value.</param>
    protected BusTestHarness(TimeProvider timeProvider)
        : base(timeProvider)
    {
    }

    /// <summary>
    /// Gets the bus control value.
    /// </summary>
    public IBusControl BusControl => _busControl ?? throw new InvalidOperationException("The bus test harness has not been started.");

    /// <summary>
    /// The address of the default bus endpoint, used as the SourceAddress for requests and published messages
    /// </summary>
    public Uri BusAddress => BusControl.Address;

    /// <summary>
    /// The name of the input queue (for the default receive endpoint)
    /// </summary>
    public abstract string InputQueueName { get; }

    /// <summary>
    /// The address of the input queue receive endpoint
    /// </summary>
    public abstract Uri InputQueueAddress { get; }

    /// <summary>
    /// The send endpoint for the default bus endpoint
    /// </summary>
    public ISendEndpoint BusSendEndpoint => _busSendEndpoint ?? throw new InvalidOperationException("The bus test harness has not been started.");

    /// <summary>
    /// The send endpoint for the input queue receive endpoint
    /// </summary>
    public ISendEndpoint InputQueueSendEndpoint => _inputQueueSendEndpoint
        ?? throw new InvalidOperationException("The bus test harness has not been started.");

    /// <summary>
    /// Gets the bus value.
    /// </summary>
    public IBus Bus => BusControl;

    /// <summary>
    /// Connects consume observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectConsumeObserver(IConsumeObserver observer) => Bus.ConnectConsumeObserver(observer);
    /// <summary>
    /// Connects publish observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer) => Bus.ConnectPublishObserver(observer);
    /// <summary>
    /// Connects send observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer) => Bus.ConnectSendObserver(observer);

    /// <summary>
    /// Gets the sent value.
    /// </summary>
    public ISentMessageList Sent => _sent?.Messages ?? throw new InvalidOperationException("The bus test harness has not been started.");
    /// <summary>
    /// Gets the cancellation token value.
    /// </summary>
    public CancellationToken CancellationToken => TestCancellationToken;
    /// <summary>
    /// Gets the consumed value.
    /// </summary>
    public IReceivedMessageList Consumed => _consumed?.Messages ?? throw new InvalidOperationException("The bus test harness has not been started.");
    /// <summary>
    /// Gets the published value.
    /// </summary>
    public IPublishedMessageList Published => _published?.Messages ?? throw new InvalidOperationException("The bus test harness has not been started.");

    /// <summary>
    /// Creates bus.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    protected abstract Task<IBusControl> CreateBusAsync();

    /// <summary>
    /// Creates request client.
    /// </summary>
    /// <typeparam name="TRequest">The t request type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public virtual IRequestClient<TRequest> CreateRequestClient<TRequest>()
        where TRequest : class
    {
        return CreateRequestClient<TRequest>(InputQueueAddress);
    }

    /// <summary>
    /// Creates request client.
    /// </summary>
    /// <typeparam name="TRequest">The t request type.</typeparam>
    /// <param name="destinationAddress">The destination address value.</param>
    /// <returns>The result of the operation.</returns>
    public virtual IRequestClient<TRequest> CreateRequestClient<TRequest>(Uri destinationAddress)
        where TRequest : class
    {
        return Bus.CreateRequestClient<TRequest>(destinationAddress, TestTimeout);
    }

    /// <summary>
    /// Connects observers.
    /// </summary>
    /// <param name="bus">The bus value.</param>
    protected virtual void ConnectObservers(IBus bus)
    {
        bus.ConnectReceiveEndpointObserver(new TestReceiveEndpointObserver(
            _published ?? throw new InvalidOperationException("The bus test harness observers have not been initialized.")));

        OnConnectObservers?.Invoke(bus);
    }

    /// <summary>
    /// Configures bus.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    protected virtual void ConfigureBus(IBusFactoryConfigurator configurator)
    {
        OnConfigureBus?.Invoke(configurator);
    }

    /// <summary>
    /// Configures receive endpoint.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    protected virtual void ConfigureReceiveEndpoint(IReceiveEndpointConfigurator configurator)
    {
        OnConfigureReceiveEndpoint?.Invoke(configurator);
    }

    /// <summary>
    /// Performs the bus configured operation.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    protected virtual void BusConfigured(IBusFactoryConfigurator configurator)
    {
        OnBusConfigured?.Invoke(configurator);
    }

    /// <summary>
    /// Occurs when pre create bus.
    /// </summary>
    public event Action<BusTestHarness>? PreCreateBus;
    /// <summary>
    /// Occurs when on configure receive endpoint.
    /// </summary>
    public event Action<IReceiveEndpointConfigurator>? OnConfigureReceiveEndpoint;
    /// <summary>
    /// Occurs when on configure bus.
    /// </summary>
    public event Action<IBusFactoryConfigurator>? OnConfigureBus;
    /// <summary>
    /// Occurs when on bus configured.
    /// </summary>
    public event Action<IBusFactoryConfigurator>? OnBusConfigured;
    /// <summary>
    /// Occurs when on connect observers.
    /// </summary>
    public event Action<IBus>? OnConnectObservers;

    /// <summary>
    /// Starts the configured component.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public virtual async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (!cancellationToken.CanBeCanceled)
            cancellationToken = TestCancellationToken;

        _received = new BusTestReceiveObserver(TestInactivityTimeout, TimeProvider);
        _received.ConnectInactivityObserver(InactivityObserver);

        _consumed = new BusTestConsumeObserver(TestTimeout, InactivityToken, TimeProvider);
        ConfigureRetention(_consumed.Messages);
        _consumed.ConnectInactivityObserver(InactivityObserver);

        _published = new BusTestPublishObserver(TestTimeout, TestInactivityTimeout, InactivityToken, TimeProvider);
        ConfigureRetention(_published.Messages);
        _published.ConnectInactivityObserver(InactivityObserver);

        _sent = new BusTestSendObserver(TestTimeout, TestInactivityTimeout, InactivityToken, TimeProvider);
        ConfigureRetention(_sent.Messages);
        _sent.ConnectInactivityObserver(InactivityObserver);

        PreCreateBus?.Invoke(this);

        _busControl = await CreateBusAsync();

        ConnectObservers(_busControl);

        await _busControl.StartAsync(cancellationToken).ConfigureAwait(false);
        _busStarted = true;

        await _received.RestartTimerAsync(cancellationToken: cancellationToken);
        await _published.RestartTimerAsync(cancellationToken: cancellationToken);
        await _sent.RestartTimerAsync(cancellationToken: cancellationToken);

        _busSendEndpoint = await GetSendEndpointAsync(_busControl.Address, cancellationToken: cancellationToken).ConfigureAwait(false);

        _inputQueueSendEndpoint = await GetSendEndpointAsync(InputQueueAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        _inputQueueSendEndpoint.ConnectSendObserver(_sent);

        _busControl.ConnectConsumeObserver(_consumed);
        _busControl.ConnectPublishObserver(_published);
        _busControl.ConnectReceiveObserver(_received);
        _busControl.ConnectSendObserver(_sent);
    }

    void ConfigureRetention<T>(IAsyncElementList<T> list)
        where T : class, IAsyncListElement
    {
        ((ITestContextRetention)list).ConfigureRetention(ContextSaveMode, MaximumSavedContexts);
    }

    /// <summary>
    /// Stops the configured component.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public virtual async Task StopAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); try
        {
            if (_busStarted && _busControl != null)
            {
                using var tokenSource = new CancellationTokenSource(TestTimeout, TimeProvider);

                await _busControl.StopAsync(tokenSource.Token).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            LogContext.Error?.Log(ex, "Stop bus faulted");
            throw;
        }
        finally
        {
            _busStarted = false;
            _busControl = null;
            _busSendEndpoint = null;
            _inputQueueSendEndpoint = null;
        }
    }

    /// <summary>
    /// Performs the clean operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public virtual async Task CleanAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
    }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    public override void Dispose()
    {
        _consumed?.Dispose();
        _published?.Dispose();
        _received?.Dispose();
        _sent?.Dispose();

        base.Dispose();
    }

    /// <summary>
    /// Gets send endpoint.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default)
    {
        return await BusControl.GetSendEndpointAsync(address, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Subscribes a message handler to the bus, which is disconnected after the message
    /// is received.
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <returns>An awaitable task completed when the message is received</returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public Task<ConsumeContext<T>> SubscribeHandlerAsync<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        TaskCompletionSource<ConsumeContext<T>> source = TaskCompletionSources.Create<ConsumeContext<T>>();

        ConnectHandle? handler = null;
        handler = Bus.ConnectHandler<T>(async context =>
        {
            handler?.Disconnect();

            source.SetResult(context);
        });

        CancellationToken effectiveCancellationToken = cancellationToken.CanBeCanceled ? cancellationToken : TestCancellationToken;
        effectiveCancellationToken.Register(() =>
        {
            handler?.Disconnect();
            source.TrySetCanceled(effectiveCancellationToken);
        });

        return source.Task;
    }

    /// <summary>
    /// Subscribes a message handler to the bus, which is disconnected after the message
    /// is received.
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="filter">A filter that only completes the task if filter is true</param>
    /// <returns>An awaitable task completed when the message is received</returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public Task<ConsumeContext<T>> SubscribeHandlerAsync<T>(Func<ConsumeContext<T>, bool> filter, CancellationToken cancellationToken = default)
        where T : class
    {
        TaskCompletionSource<ConsumeContext<T>> source = TaskCompletionSources.Create<ConsumeContext<T>>();

        ConnectHandle? handler = null;
        handler = Bus.ConnectHandler<T>(async context =>
        {
            if (filter(context))
            {
                handler?.Disconnect();

                source.SetResult(context);
            }
        });

        CancellationToken effectiveCancellationToken = cancellationToken.CanBeCanceled ? cancellationToken : TestCancellationToken;
        effectiveCancellationToken.Register(() =>
        {
            handler?.Disconnect();
            source.TrySetCanceled(effectiveCancellationToken);
        });

        return source.Task;
    }

    /// <summary>
    /// Registers a handler on the receive endpoint that is cancelled when the test is canceled
    /// and completed when the message is received.
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="configurator">The endpoint configurator</param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public Task<ConsumeContext<T>> HandledAsync<T>(IReceiveEndpointConfigurator configurator, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.ConsumeContext<T>>(cancellationToken); TaskCompletionSource<ConsumeContext<T>> source = GetTask<ConsumeContext<T>>();

        configurator.Handler<T>(async context => source.TrySetResult(context));

        return source.Task;
    }

    /// <summary>
    /// Registers a handler on the receive endpoint that is cancelled when the test is canceled
    /// and completed when the message is received.
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="configurator">The endpoint configurator</param>
    /// <param name="filter">Filter the messages based on the handled consume context</param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public Task<ConsumeContext<T>> HandledAsync<T>(IReceiveEndpointConfigurator configurator, Func<ConsumeContext<T>, bool> filter, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.ConsumeContext<T>>(cancellationToken); TaskCompletionSource<ConsumeContext<T>> source = GetTask<ConsumeContext<T>>();

        configurator.Handler<T>(async context =>
        {
            if (filter(context))
                source.TrySetResult(context);
        });

        return source.Task;
    }

    /// <summary>
    /// Registers a handler on the receive endpoint that is cancelled when the test is canceled
    /// and completed when the message is received.
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="configurator">The endpoint configurator</param>
    /// <param name="expectedCount">The expected number of messages</param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public Task<ConsumeContext<T>> HandledAsync<T>(IReceiveEndpointConfigurator configurator, int expectedCount, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.ConsumeContext<T>>(cancellationToken); TaskCompletionSource<ConsumeContext<T>> source = GetTask<ConsumeContext<T>>();

        var count = 0;
        configurator.Handler<T>(async context =>
        {
            var value = Interlocked.Increment(ref count);
            if (value == expectedCount)
                source.TrySetResult(context);
        });

        return source.Task;
    }

    /// <summary>
    /// Registers a handler on the receive endpoint that is completed after the specified handler is
    /// executed and canceled if the test is canceled.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="configurator"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public Task<ConsumeContext<T>> HandlerAsync<T>(IReceiveEndpointConfigurator configurator, MessageHandler<T> handler, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.ConsumeContext<T>>(cancellationToken); TaskCompletionSource<ConsumeContext<T>> source = GetTask<ConsumeContext<T>>();

        configurator.Handler<T>(async context =>
        {
            await handler(context).ConfigureAwait(false);
            source.TrySetResult(context);
        });

        return source.Task;
    }

    /// <summary>
    /// Registers a consumer on the receive endpoint that is cancelled when the test is canceled
    /// and completed when the message is received.
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="configurator">The endpoint configurator</param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public Task<ConsumeContext<T>> HandledByConsumerAsync<T>(IReceiveEndpointConfigurator configurator, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.ConsumeContext<T>>(cancellationToken); TaskCompletionSource<ConsumeContext<T>> source = GetTask<ConsumeContext<T>>();

        configurator.Consumer(() => new Consumer<T>(source));

        return source.Task;
    }


    class Consumer<T> :
        IConsumer<T>
        where T : class
    {
        readonly TaskCompletionSource<ConsumeContext<T>> _source;

        public Consumer(TaskCompletionSource<ConsumeContext<T>> source)
        {
            _source = source;
        }

        public Task ConsumeAsync(ConsumeContext<T> context)
        {
            _source.TrySetResult(context);

            return Task.CompletedTask;
        }
    }
}
