using System;
using System.Threading;
using ViciOne.ServiceBus.RetryPolicies;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Adds retry middleware to the message pipeline of one handler.</summary>
internal sealed class MessageRetryHandlerConfigurationObserver :
    IHandlerConfigurationObserver
{
    readonly CancellationToken _cancellationToken;
    readonly Action<IRetryConfigurator> _configure;

    /// <summary>Creates an observer for handler pipelines and a shared retry policy callback.</summary>
    /// <param name="cancellationToken">The token observed while retry delays are pending.</param>
    /// <param name="configure">The callback applied to each retry policy.</param>
    public MessageRetryHandlerConfigurationObserver(CancellationToken cancellationToken,
        Action<IRetryConfigurator> configure)
    {
        _cancellationToken = cancellationToken;
        _configure = configure ?? throw new ArgumentNullException(nameof(configure));
    }

    void IHandlerConfigurationObserver.HandlerConfigured<T>(IHandlerConfigurator<T> configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);

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
