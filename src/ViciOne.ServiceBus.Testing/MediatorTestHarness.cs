using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Mediator;
using ViciOne.ServiceBus.Testing.Implementations;

namespace ViciOne.ServiceBus.Testing;

public class MediatorTestHarness :
    AsyncTestHarness,
    IBaseTestHarness
{
    BusTestConsumeObserver? _consumed;
    IMediator? _mediator;
    BusTestPublishObserver? _published;
    BusTestSendObserver? _sent;

    public MediatorTestHarness()
        : this(TimeProvider.System)
    {
    }

    public MediatorTestHarness(TimeProvider timeProvider)
        : base(timeProvider)
    {
        TestInactivityTimeout = TimeSpan.FromSeconds(1);
    }

    public IMediator Mediator => _mediator ?? throw new InvalidOperationException("The mediator test harness has not been started.");
    public CancellationToken CancellationToken => TestCancellationToken;

    public ConnectHandle ConnectConsumeObserver(IConsumeObserver observer) => Mediator.ConnectConsumeObserver(observer);
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer) => Mediator.ConnectPublishObserver(observer);
    public ConnectHandle ConnectSendObserver(ISendObserver observer) => Mediator.ConnectSendObserver(observer);
    public IReceivedMessageList Consumed => _consumed?.Messages ?? throw new InvalidOperationException("The mediator test harness has not been started.");
    public IPublishedMessageList Published => _published?.Messages ?? throw new InvalidOperationException("The mediator test harness has not been started.");
    public ISentMessageList Sent => _sent?.Messages ?? throw new InvalidOperationException("The mediator test harness has not been started.");

    public event Action<IMediatorConfigurator>? OnConfigureMediator;

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

    protected virtual void ConfigureMediator(IMediatorConfigurator configurator)
    {
        OnConfigureMediator?.Invoke(configurator);
    }

    public override void Dispose()
    {
        _consumed?.Dispose();
        _published?.Dispose();
        _sent?.Dispose();

        base.Dispose();
    }

    public virtual IRequestClient<TRequest> CreateRequestClient<TRequest>()
        where TRequest : class
    {
        return Mediator.CreateRequestClient<TRequest>(TestTimeout);
    }

    IMediator CreateMediator()
    {
        return Bus.Factory.CreateMediator(configurator =>
        {
            ConfigureMediator(configurator);
        });
    }
}
