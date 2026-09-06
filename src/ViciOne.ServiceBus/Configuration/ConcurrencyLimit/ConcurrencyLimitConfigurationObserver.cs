using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Adds a concurrency limit filter for each message type configured on the consume pipe.</summary>
public class ConcurrencyLimitConfigurationObserver :
    ConfigurationObserver,
    IMessageConfigurationObserver
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="concurrentMessageLimit">The concurrent message limit.</param>
    /// <param name="id">The id.</param>
    public ConcurrencyLimitConfigurationObserver(IConsumePipeConfigurator configurator, int concurrentMessageLimit, string? id = null)
        : base(configurator)
    {
        Limiter = new ConcurrencyLimiter(concurrentMessageLimit, id);

        Connect(this);
    }

    /// <summary>Gets the limiter.</summary>
    public IConcurrencyLimiter Limiter { get; }

    /// <summary>Reports that message has been configured.</summary>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public void MessageConfigured<TMessage>(IConsumePipeConfigurator configurator)
        where TMessage : class
    {
        var specification = new ConcurrencyLimitConsumePipeSpecification<TMessage>(Limiter);

        configurator.AddPipeSpecification(specification);
    }
}
