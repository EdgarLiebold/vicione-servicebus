using ViciOne.ServiceBus.Observables;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Builds the mediator receive pipeline and retains observers for the materialized runtime.</summary>
internal sealed class MediatorConfiguration :
    ReceivePipeDispatcherConfiguration,
    IMediatorConfigurator,
    IMessageLimitsConfigurator
{
    readonly ConsumeObservable _consumeObservers;
    readonly IHostConfiguration _hostConfiguration;
    readonly PublishObservable _publishObservers;
    readonly SendObservable _sendObservers;
    bool _messageLimitsConfigured;

    /// <summary>Creates the primary mediator pipeline over the shared in-process host.</summary>
    /// <param name="hostConfiguration">The in-memory host shared by both mediator endpoints.</param>
    /// <param name="endpointConfiguration">The endpoint whose receive pipeline is configured.</param>
    public MediatorConfiguration(IHostConfiguration hostConfiguration, IReceiveEndpointConfiguration endpointConfiguration)
        : base(hostConfiguration, endpointConfiguration)
    {
        ArgumentNullException.ThrowIfNull(hostConfiguration);
        ArgumentNullException.ThrowIfNull(endpointConfiguration);

        _hostConfiguration = hostConfiguration;
        _consumeObservers = new ConsumeObservable();
        _publishObservers = new PublishObservable();
        _sendObservers = new SendObservable();

        if (_hostConfiguration.LogContext == null)
        {
            LogContext.ConfigureCurrentLogContext();

            _hostConfiguration.LogContext = LogContext.Current;
        }
    }

    /// <inheritdoc />
    public ConnectHandle ConnectConsumeObserver(IConsumeObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _consumeObservers.Connect(observer);
    }

    /// <inheritdoc />
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _sendObservers.Connect(observer);
    }

    /// <inheritdoc />
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _publishObservers.Connect(observer);
    }

    /// <summary>Gets the consume observers declared before the mediator is materialized.</summary>
    internal ConsumeObservable ConsumeObservers => _consumeObservers;

    /// <summary>Gets the send observers declared before the mediator is materialized.</summary>
    internal SendObservable SendObservers => _sendObservers;

    /// <summary>Gets the publish observers declared before the mediator is materialized.</summary>
    internal PublishObservable PublishObservers => _publishObservers;

    void IMessageLimitsConfigurator.SetMessageLimits(MessageLimits limits)
    {
        if (_messageLimitsConfigured)
        {
            throw new ConfigurationException(
                "Message limits for bus 'mediator': Limits is already declared. Configure exactly one Limits policy inside the mediator block.");
        }

        if (_hostConfiguration is not IMessageLimitsHostConfiguration target)
        {
            throw new ConfigurationException(
                "Message limits for bus 'mediator': The host cannot enforce receive limits. Use the built-in mediator host.");
        }

        target.SetMessageLimits(limits);
        _messageLimitsConfigured = true;
    }
}
