using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Mediator;
using ViciOne.ServiceBus.Testing.Implementations;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Provides a test harness for mediator test.</summary>
public class MediatorTestHarness :
    AsyncTestHarness,
    IBaseTestHarness
{
    BusTestConsumeObserver? _consumed;
    IMediator? _mediator;
    BusTestPublishObserver? _published;
    BusTestSendObserver? _sent;

    /// <summary>Initializes a new instance.</summary>
    public MediatorTestHarness()
        : this(TimeProvider.System)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="timeProvider">The time source used by the operation.</param>
    public MediatorTestHarness(TimeProvider timeProvider)
        : base(timeProvider)
    {
        TestInactivityTimeout = TimeSpan.FromSeconds(1);
    }

    /// <summary>Gets the mediator.</summary>
    public IMediator Mediator => _mediator ?? throw new InvalidOperationException("The mediator test harness has not been started.");
    /// <summary>Gets the cancellation token.</summary>
    public CancellationToken CancellationToken => TestCancellationToken;

    /// <summary>Connects consume observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumeObserver(IConsumeObserver observer) => Mediator.ConnectConsumeObserver(observer);
    /// <summary>Connects publish observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer) => Mediator.ConnectPublishObserver(observer);
    /// <summary>Connects send observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer) => Mediator.ConnectSendObserver(observer);
    /// <summary>Gets the consumed.</summary>
    public IReceivedMessageList Consumed => _consumed?.Messages ?? throw new InvalidOperationException("The mediator test harness has not been started.");
    /// <summary>Gets the published.</summary>
    public IPublishedMessageList Published => _published?.Messages ?? throw new InvalidOperationException("The mediator test harness has not been started.");
    /// <summary>Gets the sent.</summary>
    public ISentMessageList Sent => _sent?.Messages ?? throw new InvalidOperationException("The mediator test harness has not been started.");

    /// <summary>Occurs when on configure mediator.</summary>
    public event Action<IMediatorConfigurator>? OnConfigureMediator;

    /// <summary>Starts the configured component.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual async Task StartAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); _consumed = new BusTestConsumeObserver(TestTimeout, InactivityToken, TimeProvider);
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

    /// <summary>Configures mediator.</summary>
    /// <param name="configurator">The configurator to update.</param>
    protected virtual void ConfigureMediator(IMediatorConfigurator configurator)
    {
        OnConfigureMediator?.Invoke(configurator);
    }

    /// <summary>Releases the resources owned by this instance.</summary>
    public override void Dispose()
    {
        _consumed?.Dispose();
        _published?.Dispose();
        _sent?.Dispose();

        base.Dispose();
    }

    /// <summary>Creates request client.</summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <returns>The created request client.</returns>
    public virtual IRequestClient<TRequest> CreateRequestClient<TRequest>()
        where TRequest : class
    {
        return Mediator.CreateRequestClient<TRequest>(TestTimeout);
    }

    IMediator CreateMediator()
    {
        return Bus.Factory.CreateMediator(configurator =>
        {
            // The harness deliberately declares a real policy on behalf of its isolated test bus;
            // production mediator creation remains fail-closed when the application omits Limits.
            configurator.Limits(MessageLimits.Conservative);
            ConfigureMediator(configurator);
        });
    }
}
