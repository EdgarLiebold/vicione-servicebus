namespace ViciOne.ServiceBus.Configuration;

/// <summary>Builds the mediator receive pipeline and binds its observers to the in-memory host.</summary>
internal sealed class MediatorConfiguration :
    ReceivePipeDispatcherConfiguration,
    IMediatorConfigurator,
    IMessageLimitsConfigurator
{
    readonly IHostConfiguration _hostConfiguration;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="hostConfiguration">The in-memory host shared by both mediator endpoints.</param>
    /// <param name="endpointConfiguration">The endpoint whose receive pipeline is configured.</param>
    public MediatorConfiguration(IHostConfiguration hostConfiguration, IReceiveEndpointConfiguration endpointConfiguration)
        : base(hostConfiguration, endpointConfiguration)
    {
        _hostConfiguration = hostConfiguration;

        if (_hostConfiguration.LogContext == null)
        {
            LogContext.ConfigureCurrentLogContext();

            _hostConfiguration.LogContext = LogContext.Current;
        }
    }

    /// <summary>Connects consume observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumeObserver(IConsumeObserver observer)
    {
        return _hostConfiguration.ConnectConsumeObserver(observer);
    }

    /// <summary>Connects send observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        return _hostConfiguration.ConnectSendObserver(observer);
    }

    /// <summary>Connects publish observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        return _hostConfiguration.ConnectPublishObserver(observer);
    }

    void IMessageLimitsConfigurator.SetMessageLimits(MessageLimits limits)
    {
        if (_hostConfiguration is not IMessageLimitsHostConfiguration target)
        {
            throw new ConfigurationException(
                "Message limits for bus 'mediator': The host cannot enforce receive limits. Use the built-in mediator host.");
        }

        target.SetMessageLimits(limits);
    }
}
