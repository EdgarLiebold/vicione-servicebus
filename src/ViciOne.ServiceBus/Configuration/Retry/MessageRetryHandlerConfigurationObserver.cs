using System;
using System.Threading;
using ViciOne.ServiceBus.RetryPolicies;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Configures a message retry for a handler, on the handler configurator, which is constrained to
/// the message types for that handler, and only applies to the handler.
/// </summary>
public class MessageRetryHandlerConfigurationObserver :
    IHandlerConfigurationObserver
{
    readonly CancellationToken _cancellationToken;
    readonly Action<IRetryConfigurator> _configure;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public MessageRetryHandlerConfigurationObserver(CancellationToken cancellationToken,
        Action<IRetryConfigurator> configure)
    {
        _cancellationToken = cancellationToken;
        _configure = configure ?? throw new ArgumentNullException(nameof(configure));
    }

    void IHandlerConfigurationObserver.HandlerConfigured<T>(IHandlerConfigurator<T> configurator)
    {
        var specification = new ConsumeContextRetryPipeSpecification<ConsumeContext<T>, RetryConsumeContext<T>>(Factory, _cancellationToken);

        _configure(specification);

        configurator.AddPipeSpecification(specification);
    }

    static RetryConsumeContext<T> Factory<T>(ConsumeContext<T> context, IRetryPolicy retryPolicy, RetryContext? retryContext)
        where T : class
    {
        return new RetryConsumeContext<T>(context, retryPolicy, retryContext);
    }
}
