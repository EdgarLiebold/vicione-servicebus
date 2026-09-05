using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Testing.Implementations;

namespace ViciOne.ServiceBus.DependencyInjection.Testing;

/// <summary>
/// Provides a container test harness implementation.
/// </summary>
public class ContainerTestHarness :
    ITestHarness,
    IAsyncDisposable
{
    readonly Lazy<BusTestConsumeObserver> _consumed;
    readonly List<ConnectHandle> _handles;
    readonly Lazy<AsyncInactivityObserver> _inactivityObserver;
    readonly IServiceProvider _provider;
    readonly Lazy<BusTestPublishObserver> _published;
    readonly Lazy<BusTestReceiveObserver> _received;
    readonly Lazy<IServiceScope> _scope;
    readonly Lazy<BusTestSendObserver> _sent;
    CancellationToken _cancellationToken;
    CancellationTokenSource? _cancellationTokenSource;
    int _disposed;
    IEnumerable<IHostedService>? _hostedServices;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    /// <param name="options">The options value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    public ContainerTestHarness(IServiceProvider provider, IOptions<TestHarnessOptions> options, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(options);

        _provider = provider;
        TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

        _handles = new List<ConnectHandle>(5);

        TestTimeout = options.Value.TestTimeout;
        TestInactivityTimeout = options.Value.TestInactivityTimeout;
        ContextSaveMode = options.Value.ContextSaveMode;
        MaximumSavedContexts = options.Value.MaximumSavedContexts > 0
            ? options.Value.MaximumSavedContexts
            : throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Test harness", "unknown", "Test harness maximum saved contexts must be greater than zero.", "Correct the named configuration before starting the host"));

        _inactivityObserver = new Lazy<AsyncInactivityObserver>(
            () => new AsyncInactivityObserver(TestInactivityTimeout, CancellationToken, TimeProvider));

        _consumed = new Lazy<BusTestConsumeObserver>(() => ConfigureRetention(
            new BusTestConsumeObserver(TestTimeout, InactivityToken, TimeProvider), options.Value));
        _published = new Lazy<BusTestPublishObserver>(() => ConfigureRetention(
            new BusTestPublishObserver(TestTimeout, TestInactivityTimeout, InactivityToken, TimeProvider), options.Value));
        _received = new Lazy<BusTestReceiveObserver>(() => new BusTestReceiveObserver(TestInactivityTimeout, TimeProvider));
        _sent = new Lazy<BusTestSendObserver>(() => ConfigureRetention(
            new BusTestSendObserver(TestTimeout, TestInactivityTimeout, InactivityToken, TimeProvider), options.Value));

        _scope = new Lazy<IServiceScope>(() => _provider.CreateScope());

        provider.GetService<TestActivityListener>();
    }

    static TObserver ConfigureRetention<TObserver>(TObserver observer, TestHarnessOptions options)
        where TObserver : class
    {
        ITestContextRetention retention = observer switch
        {
            BusTestConsumeObserver consume => (ITestContextRetention)consume.Messages,
            BusTestPublishObserver publish => (ITestContextRetention)publish.Messages,
            BusTestSendObserver send => (ITestContextRetention)send.Messages,
            _ => throw new ArgumentException($"Observer {typeof(TObserver).Name} does not retain test contexts.", nameof(observer))
        };

        retention.ConfigureRetention(options.ContextSaveMode, options.MaximumSavedContexts);
        return observer;
    }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        if (_hostedServices != null)
        {
            foreach (var service in _hostedServices.Reverse())
                await service.StopAsync(CancellationToken).ConfigureAwait(false);
        }

        if (_scope.IsValueCreated)
        {
            switch (_scope.Value)
            {
                case IAsyncDisposable asyncDisposable:
                    await asyncDisposable.DisposeAsync();
                    break;
                case IDisposable disposable:
                    disposable.Dispose();
                    break;
            }
        }

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();

        _handles.ForEach(handle => handle.Disconnect());
        _handles.Clear();

        if (_consumed.IsValueCreated)
            _consumed.Value.Dispose();
        if (_published.IsValueCreated)
            _published.Value.Dispose();
        if (_received.IsValueCreated)
            _received.Value.Dispose();
        if (_sent.IsValueCreated)
            _sent.Value.Dispose();
        if (_inactivityObserver.IsValueCreated)
            _inactivityObserver.Value.Dispose();
    }

    /// <summary>
    /// Gets the inactivity task value.
    /// </summary>
    public Task InactivityTask => _inactivityObserver.Value.InactivityTask;

    /// <summary>
    /// Gets the consumed value.
    /// </summary>
    public IReceivedMessageList Consumed => _consumed.Value.Messages;
    /// <summary>
    /// Gets the published value.
    /// </summary>
    public IPublishedMessageList Published => _published.Value.Messages;
    /// <summary>
    /// Gets the sent value.
    /// </summary>
    public ISentMessageList Sent => _sent.Value.Messages;

    /// <summary>
    /// Gets the scope value.
    /// </summary>
    public IServiceScope Scope => _scope.Value;
    /// <summary>
    /// Gets the provider value.
    /// </summary>
    public IServiceProvider Provider => _provider;

    /// <summary>
    /// Gets the endpoint name formatter value.
    /// </summary>
    public IEndpointNameFormatter EndpointNameFormatter => _provider.GetService<IEndpointNameFormatter>() ?? DefaultEndpointNameFormatter.Instance;

    /// <summary>
    /// Gets the bus value.
    /// </summary>
    public IBus Bus => _provider.GetRequiredService<IBus>();

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
    /// Determines whether the current value can cel.
    /// </summary>
    public void Cancel()
    {
        _cancellationTokenSource?.Cancel();
    }

    /// <summary>
    /// Performs the force inactive operation.
    /// </summary>
    public void ForceInactive()
    {
        _inactivityObserver.Value.ForceInactive();
    }

    /// <summary>
    /// Gets consumer harness.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public IConsumerTestHarness<T> GetConsumerHarness<T>()
        where T : class, IConsumer
    {
        return _provider.GetRequiredService<IConsumerTestHarness<T>>();
    }

    /// <summary>
    /// Gets saga harness.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public ISagaTestHarness<T> GetSagaHarness<T>()
        where T : class, ISaga
    {
        return _provider.GetRequiredService<ISagaTestHarness<T>>();
    }

    /// <summary>
    /// Gets saga state machine harness.
    /// </summary>
    /// <typeparam name="TStateMachine">The t state machine type.</typeparam>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public ISagaStateMachineTestHarness<TStateMachine, T> GetSagaStateMachineHarness<TStateMachine, T>()
        where TStateMachine : class, SagaStateMachine<T>
        where T : class, SagaStateMachineInstance
    {
        return _provider.GetRequiredService<ISagaStateMachineTestHarness<TStateMachine, T>>();
    }

    /// <summary>
    /// Gets request client.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public IRequestClient<T> GetRequestClient<T>()
        where T : class
    {
        return _scope.Value.ServiceProvider.GetRequiredService<IRequestClient<T>>();
    }

    /// <summary>
    /// Gets consumer endpoint.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<ISendEndpoint> GetConsumerEndpointAsync<T>(CancellationToken cancellationToken = default)
        where T : class, IConsumer
    {
        var provider = _scope.Value.ServiceProvider.GetRequiredService<ISendEndpointProvider>();

        return provider.GetSendEndpointAsync(GetConsumerAddress<T>(), cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Gets handler endpoint.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<ISendEndpoint> GetHandlerEndpointAsync<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        return GetConsumerEndpointAsync<MessageHandlerConsumer<T>>(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Gets consumer address.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public Uri GetConsumerAddress<T>()
        where T : class, IConsumer
    {
        return new Uri($"queue:{EndpointNameFormatter.Consumer<T>()}");
    }

    /// <summary>
    /// Gets handler address.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public Uri GetHandlerAddress<T>()
        where T : class
    {
        return GetConsumerAddress<MessageHandlerConsumer<T>>();
    }

    /// <summary>
    /// Gets saga endpoint.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<ISendEndpoint> GetSagaEndpointAsync<T>(CancellationToken cancellationToken = default)
        where T : class, ISaga
    {
        var provider = _scope.Value.ServiceProvider.GetRequiredService<ISendEndpointProvider>();

        return provider.GetSendEndpointAsync(GetSagaAddress<T>(), cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Gets saga address.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public Uri GetSagaAddress<T>()
        where T : class, ISaga
    {
        return new Uri($"queue:{EndpointNameFormatter.Saga<T>()}");
    }

    /// <summary>
    /// Gets execute activity endpoint.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <typeparam name="TArguments">The t arguments type.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<ISendEndpoint> GetExecuteActivityEndpointAsync<T, TArguments>(CancellationToken cancellationToken = default)
        where T : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        var provider = _scope.Value.ServiceProvider.GetRequiredService<ISendEndpointProvider>();

        return provider.GetSendEndpointAsync(GetExecuteActivityAddress<T, TArguments>(), cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Gets execute activity address.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <typeparam name="TArguments">The t arguments type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public Uri GetExecuteActivityAddress<T, TArguments>()
        where T : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        return new Uri($"queue:{EndpointNameFormatter.ExecuteActivity<T, TArguments>()}");
    }

    /// <summary>
    /// Starts the configured component.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); _hostedServices = _provider.GetServices<IHostedService>().ToArray();
        if (!_hostedServices.Any())
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Test harness", "unknown", "The ViciOne.ServiceBus hosted service was not found.", "Correct the named configuration before starting the host"));

        foreach (var service in _hostedServices)
            await service.StartAsync(CancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets or sets the test timeout value.
    /// </summary>
    public TimeSpan TestTimeout { get; set; }
    /// <summary>
    /// Gets or sets the test inactivity timeout value.
    /// </summary>
    public TimeSpan TestInactivityTimeout { get; set; }
    /// <summary>
    /// Gets the time provider value.
    /// </summary>
    public TimeProvider TimeProvider { get; }
    /// <summary>
    /// Gets the context save mode value.
    /// </summary>
    public TestContextSaveMode ContextSaveMode { get; }
    /// <summary>
    /// Gets the maximum saved contexts value.
    /// </summary>
    public int MaximumSavedContexts { get; }

    /// <summary>
    /// CancellationToken that is cancelled when the test inactivity timeout has elapsed with no bus activity
    /// </summary>
    public CancellationToken InactivityToken => _inactivityObserver.Value.InactivityToken;

    /// <summary>
    /// CancellationToken that is canceled when the test is being aborted
    /// </summary>
    public CancellationToken CancellationToken
    {
        get
        {
            if (_cancellationToken == CancellationToken.None)
            {
                _cancellationTokenSource = new CancellationTokenSource(TestTimeout, TimeProvider);
                _cancellationToken = _cancellationTokenSource.Token;

            }

            return _cancellationToken;
        }
    }

    /// <summary>
    /// Gets task.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public TaskCompletionSource<T> GetTask<T>()
    {
        var source = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken cancellationToken = CancellationToken;
        if (cancellationToken.IsCancellationRequested)
            source.TrySetCanceled(cancellationToken);
        else
            cancellationToken.Register(static state => ((TaskCompletionSource<T>)state!).TrySetCanceled(), source);

        return source;
    }

    /// <summary>
    /// Performs the post create operation.
    /// </summary>
    /// <param name="bus">The bus value.</param>
    public void PostCreate(IBus bus)
    {
        _handles.Add(bus.ConnectReceiveEndpointObserver(new TestReceiveEndpointObserver(_published.Value)));

        _handles.Add(_consumed.Value.ConnectInactivityObserver(_inactivityObserver.Value));
        _handles.Add(_published.Value.ConnectInactivityObserver(_inactivityObserver.Value));
        _handles.Add(_received.Value.ConnectInactivityObserver(_inactivityObserver.Value));
        _handles.Add(_sent.Value.ConnectInactivityObserver(_inactivityObserver.Value));

        _handles.Add(bus.ConnectConsumeObserver(_consumed.Value));
        _handles.Add(bus.ConnectPublishObserver(_published.Value));
        _handles.Add(bus.ConnectReceiveObserver(_received.Value));
        _handles.Add(bus.ConnectSendObserver(_sent.Value));
    }

    /// <summary>
    /// Performs the post start operation.
    /// </summary>
    /// <param name="bus">The bus value.</param>
    public void PostStart(IBus bus)
    {
        _ = _received.Value.RestartTimerAsync();
        _ = _published.Value.RestartTimerAsync();
        _ = _sent.Value.RestartTimerAsync();
    }
}
