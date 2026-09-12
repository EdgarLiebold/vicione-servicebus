using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Mediator;
using ViciOne.ServiceBus.Testing.Internal;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Hosts an isolated in-process mediator and records its consume, publish, and send activity.</summary>
public class MediatorTestHarness :
    AsyncTestHarness,
    IBaseTestHarness,
    IAsyncDisposable
{
    ConnectHandle? _consumeObserverConnection;
    BusTestConsumeObserver? _consumed;
    int _disposed;
    IMediator? _mediator;
    ConnectHandle? _publishObserverConnection;
    BusTestPublishObserver? _published;
    ConnectHandle? _sendObserverConnection;
    BusTestSendObserver? _sent;

    /// <summary>Creates a mediator harness that uses the system clock.</summary>
    public MediatorTestHarness()
        : this(TimeProvider.System)
    {
    }

    /// <summary>Creates a mediator harness.</summary>
    /// <param name="timeProvider">The clock used for assertion timeouts, inactivity, and observation timestamps.</param>
    public MediatorTestHarness(TimeProvider timeProvider)
        : base(timeProvider)
    {
        TestInactivityTimeout = TimeSpan.FromSeconds(1);
    }

    /// <summary>Gets the running mediator.</summary>
    public IMediator Mediator => _mediator ?? throw new InvalidOperationException("The mediator test harness has not been started.");
    /// <inheritdoc />
    public CancellationToken CancellationToken => TestCancellationToken;

    /// <inheritdoc />
    public ConnectHandle ConnectConsumeObserver(IConsumeObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return Mediator.ConnectConsumeObserver(observer);
    }
    /// <inheritdoc />
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return Mediator.ConnectPublishObserver(observer);
    }
    /// <inheritdoc />
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return Mediator.ConnectSendObserver(observer);
    }
    /// <inheritdoc />
    public IConsumedMessageList Consumed => _consumed?.Messages ?? throw new InvalidOperationException("The mediator test harness has not been started.");
    /// <inheritdoc />
    public IPublishedMessageList Published => _published?.Messages ?? throw new InvalidOperationException("The mediator test harness has not been started.");
    /// <inheritdoc />
    public ISentMessageList Sent => _sent?.Messages ?? throw new InvalidOperationException("The mediator test harness has not been started.");

    /// <summary>Occurs while the mediator is being configured.</summary>
    public event Action<IMediatorConfigurator>? MediatorConfiguring;

    /// <summary>Creates the mediator and attaches harness observers.</summary>
    /// <param name="cancellationToken">The token checked before mediator creation.</param>
    /// <returns>A task that completes after the mediator and its observers are ready.</returns>
    public virtual async Task StartAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        if (_mediator != null)
            throw new InvalidOperationException("The mediator test harness has already been started.");

        try
        {
            _consumed = new BusTestConsumeObserver(TestTimeout, InactivityToken, TimeProvider);
            ((ITestContextRetention)_consumed.Messages).ConfigureRetention(ContextSaveMode, MaximumSavedContexts);
            _consumed.ConnectInactivityObserver(InactivityObserver);

            _published = new BusTestPublishObserver(TestTimeout, TestInactivityTimeout, InactivityToken, TimeProvider);
            ((ITestContextRetention)_published.Messages).ConfigureRetention(ContextSaveMode, MaximumSavedContexts);
            _published.ConnectInactivityObserver(InactivityObserver);

            _sent = new BusTestSendObserver(TestTimeout, TestInactivityTimeout, InactivityToken, TimeProvider);
            ((ITestContextRetention)_sent.Messages).ConfigureRetention(ContextSaveMode, MaximumSavedContexts);
            _sent.ConnectInactivityObserver(InactivityObserver);

            _mediator = CreateMediator();

            _consumeObserverConnection = _mediator.ConnectConsumeObserver(_consumed);
            _publishObserverConnection = _mediator.ConnectPublishObserver(_published);
            _sendObserverConnection = _mediator.ConnectSendObserver(_sent);
        }
        catch (Exception startupException)
        {
            var failures = new List<Exception>();
            DisposeObservers(failures);
            await DisposeMediatorAsync(failures).ConfigureAwait(false);
            if (failures.Count > 0)
            {
                failures.Insert(0, startupException);
                throw new AggregateException("Mediator test harness startup and cleanup both failed.", failures);
            }

            throw;
        }
    }

    /// <summary>Applies subscriber and derived-class configuration to a mediator.</summary>
    /// <param name="configurator">The mediator configurator.</param>
    protected virtual void ConfigureMediator(IMediatorConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        MediatorConfiguring?.Invoke(configurator);
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    /// <summary>Disconnects harness observers and releases the owned mediator and timeout resources.</summary>
    /// <returns>A task that completes after every cleanup attempt finishes.</returns>
    public virtual async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        var failures = new List<Exception>();
        DisposeObservers(failures);
        await DisposeMediatorAsync(failures).ConfigureAwait(false);

        try
        {
            base.Dispose();
        }
        catch (Exception exception)
        {
            failures.Add(exception);
        }

        if (failures.Count > 0)
            throw new AggregateException("One or more mediator test harness resources could not be released.", failures);
    }

    /// <summary>Creates a mediator request client using the harness assertion timeout.</summary>
    /// <typeparam name="TRequest">The request contract.</typeparam>
    /// <returns>The request client.</returns>
    public virtual IRequestClient<TRequest> CreateRequestClient<TRequest>()
        where TRequest : class
    {
        return Mediator.CreateRequestClient<TRequest>(new RequestTimeout(TestTimeout));
    }

    IMediator CreateMediator()
    {
        return MediatorFactory.Create(configurator =>
        {
            // The harness enforces its body policy before subscriber configuration adds handlers.
            configurator.Limits(MessageLimits.Conservative);
            ConfigureMediator(configurator);
        });
    }

    async ValueTask DisposeMediatorAsync(ICollection<Exception> failures)
    {
        IMediator? mediator = Interlocked.Exchange(ref _mediator, null);
        if (mediator == null)
            return;

        try
        {
            await mediator.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            failures.Add(exception);
        }
    }

    void DisposeObservers(ICollection<Exception> failures)
    {
        DisposeResource(ref _consumeObserverConnection, failures);
        DisposeResource(ref _publishObserverConnection, failures);
        DisposeResource(ref _sendObserverConnection, failures);

        DisposeResource(ref _consumed, failures);
        DisposeResource(ref _published, failures);
        DisposeResource(ref _sent, failures);
    }

    static void DisposeResource<T>(ref T? resource, ICollection<Exception> failures)
        where T : class, IDisposable
    {
        T? owned = resource;
        resource = null;
        if (owned == null)
            return;

        try
        {
            owned.Dispose();
        }
        catch (Exception exception)
        {
            failures.Add(exception);
        }
    }
}
