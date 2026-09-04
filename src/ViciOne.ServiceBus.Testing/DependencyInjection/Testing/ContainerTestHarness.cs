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
    CancellationTokenSource _cancellationTokenSource;
    int _disposed;
    IEnumerable<IHostedService> _hostedServices;

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
            : throw new ConfigurationException("Test harness maximum saved contexts must be greater than zero.");

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

    public Task InactivityTask => _inactivityObserver.Value.InactivityTask;

    public IReceivedMessageList Consumed => _consumed.Value.Messages;
    public IPublishedMessageList Published => _published.Value.Messages;
    public ISentMessageList Sent => _sent.Value.Messages;

    public IServiceScope Scope => _scope.Value;
    public IServiceProvider Provider => _provider;

    public IEndpointNameFormatter EndpointNameFormatter => _provider.GetService<IEndpointNameFormatter>() ?? DefaultEndpointNameFormatter.Instance;

    public IBus Bus => _provider.GetRequiredService<IBus>();

    public ConnectHandle ConnectConsumeObserver(IConsumeObserver observer) => Bus.ConnectConsumeObserver(observer);
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer) => Bus.ConnectPublishObserver(observer);
    public ConnectHandle ConnectSendObserver(ISendObserver observer) => Bus.ConnectSendObserver(observer);

    public void Cancel()
    {
        _cancellationTokenSource?.Cancel();
    }

    public void ForceInactive()
    {
        _inactivityObserver.Value.ForceInactive();
    }

    public IConsumerTestHarness<T> GetConsumerHarness<T>()
        where T : class, IConsumer
    {
        return _provider.GetRequiredService<IConsumerTestHarness<T>>();
    }

    public ISagaTestHarness<T> GetSagaHarness<T>()
        where T : class, ISaga
    {
        return _provider.GetRequiredService<ISagaTestHarness<T>>();
    }

    public ISagaStateMachineTestHarness<TStateMachine, T> GetSagaStateMachineHarness<TStateMachine, T>()
        where TStateMachine : class, SagaStateMachine<T>
        where T : class, SagaStateMachineInstance
    {
        return _provider.GetRequiredService<ISagaStateMachineTestHarness<TStateMachine, T>>();
    }

    public IRequestClient<T> GetRequestClient<T>()
        where T : class
    {
        return _scope.Value.ServiceProvider.GetRequiredService<IRequestClient<T>>();
    }

    public Task<ISendEndpoint> GetConsumerEndpoint<T>()
        where T : class, IConsumer
    {
        var provider = _scope.Value.ServiceProvider.GetRequiredService<ISendEndpointProvider>();

        return provider.GetSendEndpoint(GetConsumerAddress<T>());
    }

    public Task<ISendEndpoint> GetHandlerEndpoint<T>()
        where T : class
    {
        return GetConsumerEndpoint<MessageHandlerConsumer<T>>();
    }

    public Uri GetConsumerAddress<T>()
        where T : class, IConsumer
    {
        return new Uri($"queue:{EndpointNameFormatter.Consumer<T>()}");
    }

    public Uri GetHandlerAddress<T>()
        where T : class
    {
        return GetConsumerAddress<MessageHandlerConsumer<T>>();
    }

    public Task<ISendEndpoint> GetSagaEndpoint<T>()
        where T : class, ISaga
    {
        var provider = _scope.Value.ServiceProvider.GetRequiredService<ISendEndpointProvider>();

        return provider.GetSendEndpoint(GetSagaAddress<T>());
    }

    public Uri GetSagaAddress<T>()
        where T : class, ISaga
    {
        return new Uri($"queue:{EndpointNameFormatter.Saga<T>()}");
    }

    public Task<ISendEndpoint> GetExecuteActivityEndpoint<T, TArguments>()
        where T : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        var provider = _scope.Value.ServiceProvider.GetRequiredService<ISendEndpointProvider>();

        return provider.GetSendEndpoint(GetExecuteActivityAddress<T, TArguments>());
    }

    public Uri GetExecuteActivityAddress<T, TArguments>()
        where T : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        return new Uri($"queue:{EndpointNameFormatter.ExecuteActivity<T, TArguments>()}");
    }

    public async Task Start()
    {
        _hostedServices = _provider.GetServices<IHostedService>().ToArray();
        if (!_hostedServices.Any())
            throw new ConfigurationException("The ViciOne.ServiceBus hosted service was not found.");

        foreach (var service in _hostedServices)
            await service.StartAsync(CancellationToken).ConfigureAwait(false);
    }

    public TimeSpan TestTimeout { get; set; }
    public TimeSpan TestInactivityTimeout { get; set; }
    public TimeProvider TimeProvider { get; }
    public TestContextSaveMode ContextSaveMode { get; }
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

    public void PostStart(IBus bus)
    {
        _ = _received.Value.RestartTimer();
        _ = _published.Value.RestartTimer();
        _ = _sent.Value.RestartTimer();
    }
}
