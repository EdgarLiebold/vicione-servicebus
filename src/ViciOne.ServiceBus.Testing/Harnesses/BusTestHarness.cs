using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Testing.Internal;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Hosts one service-bus instance and records its send, publish, receive, and consume activity for tests.</summary>
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

    /// <summary>Initializes a harness that uses the system time provider.</summary>
    protected BusTestHarness()
    {
    }

    /// <summary>Initializes a harness that uses the specified time provider.</summary>
    /// <param name="timeProvider">The time provider used by harness timers.</param>
    protected BusTestHarness(TimeProvider timeProvider)
        : base(timeProvider)
    {
    }

    /// <summary>Gets the running bus control.</summary>
    public IBusControl BusControl => _busControl ?? throw new InvalidOperationException("The bus test harness has not been started.");

    /// <summary>Gets the default bus address used as the source of requests and published messages.</summary>
    public Uri BusAddress => BusControl.Address;

    /// <summary>Gets the name of the default receive endpoint's input queue.</summary>
    public abstract string InputQueueName { get; }

    /// <summary>Gets the address of the default receive endpoint's input queue.</summary>
    public abstract Uri InputQueueAddress { get; }

    /// <summary>Gets the send endpoint for the default bus address.</summary>
    public ISendEndpoint BusSendEndpoint => _busSendEndpoint ?? throw new InvalidOperationException("The bus test harness has not been started.");

    /// <summary>Gets the send endpoint for the default receive endpoint's input queue.</summary>
    public ISendEndpoint InputQueueSendEndpoint => _inputQueueSendEndpoint
        ?? throw new InvalidOperationException("The bus test harness has not been started.");

    /// <summary>Gets the running bus.</summary>
    public IBus Bus => BusControl;

    /// <summary>Connects an observer to consume operations performed by the running bus.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumeObserver(IConsumeObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return Bus.ConnectConsumeObserver(observer);
    }

    /// <summary>Connects an observer to publish operations performed by the running bus.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return Bus.ConnectPublishObserver(observer);
    }

    /// <summary>Connects an observer to send operations performed by the running bus.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return Bus.ConnectSendObserver(observer);
    }

    /// <summary>Gets messages sent since the harness was started.</summary>
    public ISentMessageList Sent => _sent?.Messages ?? throw new InvalidOperationException("The bus test harness has not been started.");

    /// <summary>Gets the current test-scope cancellation token.</summary>
    public CancellationToken CancellationToken => TestCancellationToken;

    /// <summary>Gets messages consumed since the harness was started.</summary>
    public IConsumedMessageList Consumed => _consumed?.Messages ?? throw new InvalidOperationException("The bus test harness has not been started.");

    /// <summary>Gets messages published since the harness was started.</summary>
    public IPublishedMessageList Published => _published?.Messages ?? throw new InvalidOperationException("The bus test harness has not been started.");

    /// <summary>Creates the provider-specific bus controlled by this harness.</summary>
    /// <param name="cancellationToken">The token that cancels asynchronous provider preparation.</param>
    /// <returns>A task that produces the configured bus control.</returns>
    protected abstract Task<IBusControl> CreateBusAsync(CancellationToken cancellationToken);

    /// <summary>Creates a request client that sends to the default input queue.</summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <returns>A request client configured with the harness timeout.</returns>
    public virtual IRequestClient<TRequest> CreateRequestClient<TRequest>()
        where TRequest : class
    {
        return CreateRequestClient<TRequest>(InputQueueAddress);
    }

    /// <summary>Creates a request client that sends to the specified destination.</summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <param name="destinationAddress">The destination address.</param>
    /// <returns>A request client configured with the harness timeout.</returns>
    public virtual IRequestClient<TRequest> CreateRequestClient<TRequest>(Uri destinationAddress)
        where TRequest : class
    {
        ArgumentNullException.ThrowIfNull(destinationAddress);
        return Bus.CreateRequestClient<TRequest>(destinationAddress, new RequestTimeout(TestTimeout));
    }

    /// <summary>Connects provider-independent observers to the configured bus.</summary>
    /// <param name="bus">The bus to observe.</param>
    protected virtual void ConnectObservers(IBus bus)
    {
        bus.ConnectReceiveEndpointObserver(new TestReceiveEndpointObserver(
            _published ?? throw new InvalidOperationException("The bus test harness observers have not been initialized.")));

        ObserversConnecting?.Invoke(bus);
    }

    /// <summary>Applies subscriber-provided configuration to the bus.</summary>
    /// <param name="configurator">The bus configurator to update.</param>
    protected virtual void ConfigureBus(IBusFactoryConfigurator configurator)
    {
        BusConfiguring?.Invoke(configurator);
    }

    /// <summary>Applies subscriber-provided configuration to the default receive endpoint.</summary>
    /// <param name="configurator">The receive-endpoint configurator to update.</param>
    protected virtual void ConfigureReceiveEndpoint(IReceiveEndpointConfigurator configurator)
    {
        ReceiveEndpointConfiguring?.Invoke(configurator);
    }

    /// <summary>Notifies subscribers after bus configuration has completed.</summary>
    /// <param name="configurator">The completed bus configurator.</param>
    protected virtual void NotifyBusConfigured(IBusFactoryConfigurator configurator)
    {
        BusConfigured?.Invoke(configurator);
    }

    /// <summary>Occurs immediately before the provider-specific bus is created.</summary>
    public event Action<BusTestHarness>? BusCreating;

    /// <summary>Occurs while the default receive endpoint is being configured.</summary>
    public event Action<IReceiveEndpointConfigurator>? ReceiveEndpointConfiguring;

    /// <summary>Occurs while the bus is being configured.</summary>
    public event Action<IBusFactoryConfigurator>? BusConfiguring;

    /// <summary>Occurs after bus configuration has completed.</summary>
    public event Action<IBusFactoryConfigurator>? BusConfigured;

    /// <summary>Occurs while observers are being connected to the configured bus.</summary>
    public event Action<IBus>? ObserversConnecting;

    /// <summary>Starts the configured component.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_busControl != null)
            throw new InvalidOperationException("The bus test harness has already been started.");

        if (!cancellationToken.CanBeCanceled)
            cancellationToken = TestCancellationToken;

        cancellationToken.ThrowIfCancellationRequested();
        DisposeObservers();

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

        IBusControl? busControl = null;
        try
        {
            BusCreating?.Invoke(this);

            busControl = await CreateBusAsync(cancellationToken).ConfigureAwait(false);
            ConnectObservers(busControl);

            await busControl.StartAsync(cancellationToken).ConfigureAwait(false);

            await _received.RestartTimerAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            await _published.RestartTimerAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            await _sent.RestartTimerAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            ISendEndpoint busSendEndpoint = await busControl.GetSendEndpointAsync(busControl.Address, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            ISendEndpoint inputQueueSendEndpoint = await busControl.GetSendEndpointAsync(InputQueueAddress, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            inputQueueSendEndpoint.ConnectSendObserver(_sent);
            busControl.ConnectConsumeObserver(_consumed);
            busControl.ConnectPublishObserver(_published);
            busControl.ConnectReceiveObserver(_received);
            busControl.ConnectSendObserver(_sent);

            _busControl = busControl;
            _busSendEndpoint = busSendEndpoint;
            _inputQueueSendEndpoint = inputQueueSendEndpoint;
            _busStarted = true;
        }
        catch
        {
            if (busControl != null)
                await TryStopAfterFailedStartAsync(busControl).ConfigureAwait(false);

            ResetBusState();
            DisposeObservers();
            throw;
        }
    }

    void ConfigureRetention<T>(IAsyncElementList<T> list)
        where T : class, IAsyncListElement
    {
        ((ITestContextRetention)list).ConfigureRetention(ContextSaveMode, MaximumSavedContexts);
    }

    /// <summary>Stops the configured component.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual async Task StopAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            if (_busStarted && _busControl != null)
            {
                using var timeoutSource = new CancellationTokenSource(TestTimeout, TimeProvider);
                using CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    timeoutSource.Token);

                await _busControl.StopAsync(linkedSource.Token).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            LogContext.Error?.Log(ex, "Stop bus faulted");
            throw;
        }
        finally
        {
            ResetBusState();
        }
    }

    /// <summary>Releases provider-specific test artifacts. The base harness owns no external artifacts.</summary>
    /// <param name="cancellationToken">The token that cancels provider cleanup.</param>
    /// <returns>A task that completes when provider cleanup has finished.</returns>
    public virtual Task CleanAsync(CancellationToken cancellationToken = default)
    {
        return cancellationToken.IsCancellationRequested
            ? Task.FromCanceled(cancellationToken)
            : Task.CompletedTask;
    }

    /// <summary>Releases the resources owned by this instance.</summary>
    public override void Dispose()
    {
        DisposeObservers();

        base.Dispose();
    }

    /// <summary>Gets a send endpoint for the specified address from the running bus.</summary>
    /// <param name="address">The destination address.</param>
    /// <param name="cancellationToken">The token that cancels endpoint resolution.</param>
    /// <returns>A task whose result is the send endpoint.</returns>
    public Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(address);

        return BusControl.GetSendEndpointAsync(address, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Subscribes a message handler to the bus, which is disconnected after the message
    /// is received.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>An awaitable task completed when the message is received.</returns>
    public Task<ConsumeContext<T>> SubscribeHandlerAsync<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        TaskCompletionSource<ConsumeContext<T>> source = CreateTask<ConsumeContext<T>>(cancellationToken);

        ConnectHandle handler = Bus.ConnectHandler<T>(context =>
        {
            source.TrySetResult(context);

            return Task.CompletedTask;
        });

        DisconnectWhenCompleted(source.Task, handler);

        return source.Task;
    }

    /// <summary>
    /// Subscribes a message handler to the bus, which is disconnected after the message
    /// is received.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="filter">A filter that only completes the task if filter is true.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>An awaitable task completed when the message is received.</returns>
    public Task<ConsumeContext<T>> SubscribeHandlerAsync<T>(Func<ConsumeContext<T>, bool> filter, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(filter);
        TaskCompletionSource<ConsumeContext<T>> source = CreateTask<ConsumeContext<T>>(cancellationToken);

        ConnectHandle handler = Bus.ConnectHandler<T>(context =>
        {
            if (filter(context))
                source.TrySetResult(context);

            return Task.CompletedTask;
        });

        DisconnectWhenCompleted(source.Task, handler);

        return source.Task;
    }

    /// <summary>
    /// Registers a handler on the receive endpoint that is cancelled when the test is canceled
    /// and completed when the message is received.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="configurator">The endpoint configurator.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the handled outcome.</returns>
    public Task<ConsumeContext<T>> HandledAsync<T>(IReceiveEndpointConfigurator configurator, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<ConsumeContext<T>>(cancellationToken);

        TaskCompletionSource<ConsumeContext<T>> source = CreateTask<ConsumeContext<T>>(cancellationToken);

        configurator.Handler<T>(context =>
        {
            source.TrySetResult(context);
            return Task.CompletedTask;
        });

        return source.Task;
    }

    /// <summary>
    /// Registers a handler on the receive endpoint that is cancelled when the test is canceled
    /// and completed when the message is received.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="configurator">The endpoint configurator.</param>
    /// <param name="filter">Filter the messages based on the handled consume context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the handled outcome.</returns>
    public Task<ConsumeContext<T>> HandledAsync<T>(IReceiveEndpointConfigurator configurator, Func<ConsumeContext<T>, bool> filter, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(filter);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<ConsumeContext<T>>(cancellationToken);

        TaskCompletionSource<ConsumeContext<T>> source = CreateTask<ConsumeContext<T>>(cancellationToken);

        configurator.Handler<T>(context =>
        {
            if (filter(context))
                source.TrySetResult(context);

            return Task.CompletedTask;
        });

        return source.Task;
    }

    /// <summary>
    /// Registers a handler on the receive endpoint that is cancelled when the test is canceled
    /// and completed when the message is received.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="configurator">The endpoint configurator.</param>
    /// <param name="expectedCount">The expected number of messages.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the handled outcome.</returns>
    public Task<ConsumeContext<T>> HandledAsync<T>(IReceiveEndpointConfigurator configurator, int expectedCount, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(expectedCount);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<ConsumeContext<T>>(cancellationToken);

        TaskCompletionSource<ConsumeContext<T>> source = CreateTask<ConsumeContext<T>>(cancellationToken);

        var count = 0;
        configurator.Handler<T>(context =>
        {
            var value = Interlocked.Increment(ref count);
            if (value == expectedCount)
                source.TrySetResult(context);

            return Task.CompletedTask;
        });

        return source.Task;
    }

    /// <summary>
    /// Registers a handler on the receive endpoint that is completed after the specified handler is
    /// executed and canceled if the test is canceled.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="handler">The handler.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the handler outcome.</returns>
    public Task<ConsumeContext<T>> HandlerAsync<T>(IReceiveEndpointConfigurator configurator, MessageHandler<T> handler, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(handler);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<ConsumeContext<T>>(cancellationToken);

        TaskCompletionSource<ConsumeContext<T>> source = CreateTask<ConsumeContext<T>>(cancellationToken);

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
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="configurator">The endpoint configurator.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the handled by consumer outcome.</returns>
    public Task<ConsumeContext<T>> HandledByConsumerAsync<T>(IReceiveEndpointConfigurator configurator, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<ConsumeContext<T>>(cancellationToken);

        TaskCompletionSource<ConsumeContext<T>> source = CreateTask<ConsumeContext<T>>(cancellationToken);

        configurator.Consumer(() => new Consumer<T>(source));

        return source.Task;
    }

    static void DisconnectWhenCompleted(Task task, ConnectHandle handler)
    {
        _ = task.ContinueWith(
            static (_, state) => ((ConnectHandle)state!).Disconnect(),
            handler,
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    async Task TryStopAfterFailedStartAsync(IBusControl busControl)
    {
        try
        {
            using var timeoutSource = new CancellationTokenSource(TestTimeout, TimeProvider);
            await busControl.StopAsync(timeoutSource.Token).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            LogContext.Error?.Log(exception, "Stopping the bus after a failed harness start also faulted");
        }
    }

    void ResetBusState()
    {
        _busStarted = false;
        _busControl = null;
        _busSendEndpoint = null;
        _inputQueueSendEndpoint = null;
    }

    void DisposeObservers()
    {
        _consumed?.Dispose();
        _published?.Dispose();
        _received?.Dispose();
        _sent?.Dispose();

        _consumed = null;
        _published = null;
        _received = null;
        _sent = null;
    }

    sealed class Consumer<TMessage> :
        IConsumer<TMessage>
        where TMessage : class
    {
        readonly TaskCompletionSource<ConsumeContext<TMessage>> _source;

        public Consumer(TaskCompletionSource<ConsumeContext<TMessage>> source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public Task ConsumeAsync(ConsumeContext<TMessage> context)
        {
            ArgumentNullException.ThrowIfNull(context);
            _source.TrySetResult(context);

            return Task.CompletedTask;
        }
    }
}
