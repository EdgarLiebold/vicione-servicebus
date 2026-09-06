namespace ViciOne.ServiceBus.Configuration;

/// <summary>Stores and validates mediator configuration.</summary>
public class MediatorConfiguration :
    ReceivePipeDispatcherConfiguration,
    IMediatorConfigurator,
    IMessageLimitsConfigurator
{
    readonly IHostConfiguration _hostConfiguration;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="hostConfiguration">The host configuration.</param>
    /// <param name="endpointConfiguration">The endpoint configuration.</param>
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
