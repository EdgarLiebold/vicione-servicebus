namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a mediator configuration implementation.
/// </summary>
public class MediatorConfiguration :
    ReceivePipeDispatcherConfiguration,
    IMediatorConfigurator
{
    readonly IHostConfiguration _hostConfiguration;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="endpointConfiguration">The endpoint configuration value.</param>
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

    /// <summary>
    /// Connects consume observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectConsumeObserver(IConsumeObserver observer)
    {
        return _hostConfiguration.ConnectConsumeObserver(observer);
    }

    /// <summary>
    /// Connects send observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        return _hostConfiguration.ConnectSendObserver(observer);
    }

    /// <summary>
    /// Connects publish observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        return _hostConfiguration.ConnectPublishObserver(observer);
    }
}
