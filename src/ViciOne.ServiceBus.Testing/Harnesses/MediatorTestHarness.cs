using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Mediator;
using ViciOne.ServiceBus.Testing.Internal;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Hosts an isolated in-process mediator and records its consume, publish, and send activity.</summary>
public class MediatorTestHarness :
    AsyncTestHarness,
    IBaseTestHarness
{
    BusTestConsumeObserver? _consumed;
    IMediator? _mediator;
    BusTestPublishObserver? _published;
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
    /// <returns>A completed task after the mediator is ready.</returns>
    public virtual Task StartAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
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

            _mediator.ConnectConsumeObserver(_consumed);
            _mediator.ConnectPublishObserver(_published);
            _mediator.ConnectSendObserver(_sent);
        }
        catch
        {
            DisposeObservers();
            throw;
        }

        return Task.CompletedTask;
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
        DisposeObservers();

        base.Dispose();
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
        return Bus.Factory.CreateMediator(configurator =>
        {
            // An isolated harness owns its message limit policy just as an application-owned bus does.
            configurator.Limits(MessageLimits.Conservative);
            ConfigureMediator(configurator);
        });
    }

    void DisposeObservers()
    {
        _consumed?.Dispose();
        _published?.Dispose();
        _sent?.Dispose();

        _consumed = null;
        _published = null;
        _sent = null;
        _mediator = null;
    }
}
