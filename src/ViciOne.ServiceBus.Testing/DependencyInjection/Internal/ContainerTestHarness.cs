using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Testing;

namespace ViciOne.ServiceBus.Testing.Internal;

/// <summary>Coordinates the service-provider-owned bus, observers, scope, and hosted-service lifecycle for an <see cref="ITestHarness" />.</summary>
internal sealed class ContainerTestHarness :
    ITestHarness,
    IAsyncDisposable
{
    readonly Lazy<BusTestConsumeObserver> _consumed;
    readonly List<ConnectHandle> _handles;
    readonly Lazy<AsyncInactivityObserver> _inactivityObserver;
    readonly object _lifecycleLock = new();
    readonly IServiceProvider _provider;
    readonly Lazy<BusTestPublishObserver> _published;
    readonly Lazy<BusTestReceiveObserver> _received;
    readonly Lazy<IServiceScope> _scope;
    readonly Lazy<BusTestSendObserver> _sent;
    readonly SemaphoreSlim _lifecycleGate = new(1, 1);
    CancellationToken _cancellationToken;
    CancellationTokenSource? _cancellationTokenSource;
    int _disposed;
    IReadOnlyList<IHostedService>? _hostedServices;
    int _started;
    TimeSpan _testInactivityTimeout;
    TimeSpan _testTimeout;

    /// <summary>Creates a harness over the supplied service provider and validated test policy.</summary>
    /// <param name="provider">The service provider that owns the bus and its dependencies.</param>
    /// <param name="options">The observation timeout and retention policy.</param>
    /// <param name="timeProvider">The clock used for timeouts and inactivity detection.</param>
    public ContainerTestHarness(IServiceProvider provider, IOptions<TestHarnessOptions> options, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(options);

        _provider = provider;
        TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

        _handles = new List<ConnectHandle>(9);

        TestTimeout = options.Value.TestTimeout;
        TestInactivityTimeout = options.Value.TestInactivityTimeout;
        ContextSaveMode = Enum.IsDefined(options.Value.ContextSaveMode)
            ? options.Value.ContextSaveMode
            : throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Test harness", "unknown", "The test harness context save mode is not defined.", "Select a valid test context save mode before starting the host"));
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

    /// <summary>Stops hosted services and releases every scope, connection, observer, and timer owned by the harness.</summary>
    /// <returns>A task that completes after all cleanup attempts finish.</returns>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        var failures = new List<Exception>();

        await _lifecycleGate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            await StopCoreAsync(CancellationToken.None, failures).ConfigureAwait(false);
        }
        finally
        {
            _lifecycleGate.Release();
        }

        if (_scope.IsValueCreated)
        {
            try
            {
                switch (_scope.Value)
                {
                    case IAsyncDisposable asyncDisposable:
                        await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                        break;
                    case IDisposable disposable:
                        disposable.Dispose();
                        break;
                }
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
        }

        try
        {
            lock (_lifecycleLock)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                _cancellationToken = default;
            }
        }
        catch (Exception exception)
        {
            failures.Add(exception);
        }

        foreach (ConnectHandle handle in _handles)
        {
            try
            {
                handle.Disconnect();
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
        }
        _handles.Clear();

        DisposeObserver(_consumed, failures);
        DisposeObserver(_published, failures);
        DisposeObserver(_received, failures);
        DisposeObserver(_sent, failures);
        DisposeObserver(_inactivityObserver, failures);

        if (failures.Count > 0)
            throw new AggregateException("One or more test-harness resources could not be released.", failures);
    }

    static void DisposeObserver<TObserver>(Lazy<TObserver> observer, ICollection<Exception> failures)
        where TObserver : IDisposable
    {
        if (!observer.IsValueCreated)
            return;

        try
        {
            observer.Value.Dispose();
        }
        catch (Exception exception)
        {
            failures.Add(exception);
        }
    }

    /// <inheritdoc />
    public Task InactivityTask => _inactivityObserver.Value.InactivityTask;

    /// <inheritdoc />
    public IConsumedMessageList Consumed => _consumed.Value.Messages;
    /// <inheritdoc />
    public IPublishedMessageList Published => _published.Value.Messages;
    /// <inheritdoc />
    public ISentMessageList Sent => _sent.Value.Messages;

    /// <inheritdoc />
    public IServiceScope Scope => _scope.Value;
    /// <inheritdoc />
    public IServiceProvider Provider => _provider;

    /// <inheritdoc />
    public IEndpointNameFormatter EndpointNameFormatter => _provider.GetService<IEndpointNameFormatter>() ?? DefaultEndpointNameFormatter.Instance;

    /// <inheritdoc />
    public IBus Bus => _provider.GetRequiredService<IBus>();

    /// <inheritdoc />
    public ConnectHandle ConnectConsumeObserver(IConsumeObserver observer) => Bus.ConnectConsumeObserver(observer);
    /// <inheritdoc />
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer) => Bus.ConnectPublishObserver(observer);
    /// <inheritdoc />
    public ConnectHandle ConnectSendObserver(ISendObserver observer) => Bus.ConnectSendObserver(observer);

    /// <inheritdoc />
    public void Cancel()
    {
        CancellationTokenSource source;
        lock (_lifecycleLock)
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
            source = GetOrCreateCancellationSource();
        }

        try
        {
            source.Cancel();
        }
        catch (ObjectDisposedException) when (Volatile.Read(ref _disposed) != 0)
        {
        }
    }

    /// <inheritdoc />
    public void ForceInactive()
    {
        _inactivityObserver.Value.ForceInactive();
    }

    /// <inheritdoc />
    public IConsumerTestHarness<T> GetConsumerHarness<T>()
        where T : class, IConsumer
    {
        return _provider.GetRequiredService<IConsumerTestHarness<T>>();
    }

    /// <inheritdoc />
    public ISagaTestHarness<T> GetSagaHarness<T>()
        where T : class, ISaga
    {
        return _provider.GetRequiredService<ISagaTestHarness<T>>();
    }

    /// <inheritdoc />
    public ISagaStateMachineTestHarness<TStateMachine, T> GetSagaStateMachineHarness<TStateMachine, T>()
        where TStateMachine : class, SagaStateMachine<T>
        where T : class, SagaStateMachineInstance
    {
        return _provider.GetRequiredService<ISagaStateMachineTestHarness<TStateMachine, T>>();
    }

    /// <inheritdoc />
    public IRequestClient<T> CreateRequestClient<T>()
        where T : class
    {
        return _scope.Value.ServiceProvider.GetRequiredService<IRequestClient<T>>();
    }

    /// <inheritdoc />
    public Task<ISendEndpoint> GetConsumerEndpointAsync<T>(CancellationToken cancellationToken = default)
        where T : class, IConsumer
    {
        var provider = _scope.Value.ServiceProvider.GetRequiredService<ISendEndpointProvider>();

        return provider.GetSendEndpointAsync(GetConsumerAddress<T>(), cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public Task<ISendEndpoint> GetHandlerEndpointAsync<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        return GetConsumerEndpointAsync<MessageHandlerConsumer<T>>(cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public Uri GetConsumerAddress<T>()
        where T : class, IConsumer
    {
        return new Uri($"queue:{EndpointNameFormatter.Consumer<T>()}");
    }

    /// <inheritdoc />
    public Uri GetHandlerAddress<T>()
        where T : class
    {
        return new Uri($"queue:{EndpointNameFormatter.Message<T>()}");
    }

    /// <inheritdoc />
    public Task<ISendEndpoint> GetSagaEndpointAsync<T>(CancellationToken cancellationToken = default)
        where T : class, ISaga
    {
        var provider = _scope.Value.ServiceProvider.GetRequiredService<ISendEndpointProvider>();

        return provider.GetSendEndpointAsync(GetSagaAddress<T>(), cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public Uri GetSagaAddress<T>()
        where T : class, ISaga
    {
        return new Uri($"queue:{EndpointNameFormatter.Saga<T>()}");
    }

    /// <inheritdoc />
    public Task<ISendEndpoint> GetExecuteActivityEndpointAsync<T, TArguments>(CancellationToken cancellationToken = default)
        where T : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        var provider = _scope.Value.ServiceProvider.GetRequiredService<ISendEndpointProvider>();

        return provider.GetSendEndpointAsync(GetExecuteActivityAddress<T, TArguments>(), cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public Uri GetExecuteActivityAddress<T, TArguments>()
        where T : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        return new Uri($"queue:{EndpointNameFormatter.ExecuteActivity<T, TArguments>()}");
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        if (!cancellationToken.CanBeCanceled)
            cancellationToken = CancellationToken;
        cancellationToken.ThrowIfCancellationRequested();

        await _lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
            if (Volatile.Read(ref _started) != 0)
                throw new InvalidOperationException("The test harness has already been started.");

            IHostedService[] services = GetHostedServices();
            await StartCoreAsync(services, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        cancellationToken.ThrowIfCancellationRequested();

        await _lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
            var failures = new List<Exception>();
            await StopCoreAsync(cancellationToken, failures).ConfigureAwait(false);
            ThrowLifecycleFailures("One or more test-harness services could not be stopped.", failures);
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    /// <inheritdoc />
    public async Task RestartAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        cancellationToken.ThrowIfCancellationRequested();

        await _lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
            if (Volatile.Read(ref _started) == 0)
                throw new InvalidOperationException("The test harness has not been started.");

            IHostedService[] services = _hostedServices?.ToArray()
                ?? throw new InvalidOperationException("The test harness has no started hosted services to restart.");
            var failures = new List<Exception>();
            await StopCoreAsync(cancellationToken, failures).ConfigureAwait(false);
            ThrowLifecycleFailures("One or more test-harness services could not be stopped for restart.", failures);

            await StartCoreAsync(services, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    IHostedService[] GetHostedServices()
    {
        IHostedService[] services = _provider.GetServices<IHostedService>().ToArray();
        if (services.Length == 0)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Test harness", "unknown", "The ViciOne.ServiceBus hosted service was not found.", "Correct the named configuration before starting the host"));

        return services;
    }

    async Task StartCoreAsync(IHostedService[] services, CancellationToken cancellationToken)
    {
        var startedServices = new List<IHostedService>(services.Length);
        try
        {
            foreach (IHostedService service in services)
            {
                await service.StartAsync(cancellationToken).ConfigureAwait(false);
                startedServices.Add(service);
            }

            _hostedServices = startedServices;
            Volatile.Write(ref _started, 1);
        }
        catch (Exception startException)
        {
            var failures = new List<Exception> { startException };
            foreach (IHostedService service in startedServices.AsEnumerable().Reverse())
            {
                try
                {
                    await service.StopAsync(CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception cleanupException)
                {
                    failures.Add(cleanupException);
                }
            }

            _hostedServices = null;
            Volatile.Write(ref _started, 0);
            ThrowLifecycleFailures("The test harness failed to start and at least one started service could not be stopped.", failures);
        }
    }

    async Task StopCoreAsync(CancellationToken cancellationToken, ICollection<Exception> failures)
    {
        if (_hostedServices == null || _hostedServices.Count == 0)
        {
            Volatile.Write(ref _started, 0);
            return;
        }

        var failedServices = new List<IHostedService>();
        foreach (IHostedService service in _hostedServices.Reverse())
        {
            try
            {
                await service.StopAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                failures.Add(exception);
                failedServices.Add(service);
            }
        }

        failedServices.Reverse();
        _hostedServices = failedServices.Count == 0 ? null : failedServices;
        Volatile.Write(ref _started, failedServices.Count == 0 ? 0 : 1);
    }

    static void ThrowLifecycleFailures(string message, IReadOnlyCollection<Exception> failures)
    {
        if (failures.Count == 0)
            return;
        if (failures.Count == 1)
        {
            ExceptionDispatchInfo.Capture(failures.Single()).Throw();
            return;
        }

        throw new AggregateException(message, failures);
    }

    /// <inheritdoc />
    public TimeSpan TestTimeout
    {
        get => _testTimeout;
        set => _testTimeout = ValidatePositiveTimeout(value);
    }

    /// <inheritdoc />
    public TimeSpan TestInactivityTimeout
    {
        get => _testInactivityTimeout;
        set => _testInactivityTimeout = ValidatePositiveTimeout(value);
    }

    /// <inheritdoc />
    public TimeProvider TimeProvider { get; }
    /// <inheritdoc />
    public TestContextSaveMode ContextSaveMode { get; }
    /// <inheritdoc />
    public int MaximumSavedContexts { get; }

    /// <inheritdoc />
    public CancellationToken InactivityToken => _inactivityObserver.Value.InactivityToken;

    /// <inheritdoc />
    public CancellationToken CancellationToken
    {
        get
        {
            lock (_lifecycleLock)
            {
                ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
                GetOrCreateCancellationSource();
                return _cancellationToken;
            }
        }
    }

    /// <inheritdoc />
    public TaskCompletionSource<T> CreateTaskCompletionSource<T>(CancellationToken cancellationToken = default)
    {
        var source = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken harnessCancellationToken = CancellationToken;
        RegisterCancellation(source, harnessCancellationToken);
        if (cancellationToken.CanBeCanceled && cancellationToken != harnessCancellationToken)
            RegisterCancellation(source, cancellationToken);

        return source;
    }

    static void RegisterCancellation<T>(TaskCompletionSource<T> source, CancellationToken cancellationToken)
    {
        CancellationTokenRegistration registration = cancellationToken.Register(
            static state =>
            {
                var registrationState = ((TaskCompletionSource<T> Source, CancellationToken Token))state!;
                registrationState.Source.TrySetCanceled(registrationState.Token);
            },
            (source, cancellationToken));

        _ = source.Task.ContinueWith(
            static (_, state) => ((CancellationTokenRegistration)state!).Dispose(),
            registration,
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    CancellationTokenSource GetOrCreateCancellationSource()
    {
        if (_cancellationTokenSource != null)
            return _cancellationTokenSource;

        _cancellationTokenSource = new CancellationTokenSource(TestTimeout, TimeProvider);
        _cancellationToken = _cancellationTokenSource.Token;
        return _cancellationTokenSource;
    }

    /// <summary>Connects the harness observers after the bus has been created.</summary>
    /// <param name="bus">The created bus.</param>
    public void PostCreate(IBus bus)
    {
        ArgumentNullException.ThrowIfNull(bus);

        _ = _provider
            .GetServices<IContainerTestHarnessObservationRegistration>()
            .ToArray();

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

    /// <summary>Restarts inactivity windows after the bus has started.</summary>
    /// <returns>A task that completes when every observer has restarted its window.</returns>
    public Task PostStartAsync()
    {
        return Task.WhenAll(
            _received.Value.RestartTimerAsync(),
            _published.Value.RestartTimerAsync(),
            _sent.Value.RestartTimerAsync());
    }

    static TimeSpan ValidatePositiveTimeout(TimeSpan value)
    {
        return value > TimeSpan.Zero && value != Timeout.InfiniteTimeSpan
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), value, "The timeout must be greater than zero.");
    }
}
