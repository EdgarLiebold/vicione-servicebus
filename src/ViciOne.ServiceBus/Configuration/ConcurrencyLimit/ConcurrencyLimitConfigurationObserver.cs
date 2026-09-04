using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Adds a concurrency limit filter for each message type configured on the consume pipe
/// </summary>
public class ConcurrencyLimitConfigurationObserver :
    ConfigurationObserver,
    IMessageConfigurationObserver
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="concurrentMessageLimit">The concurrent message limit value.</param>
    /// <param name="id">The id value.</param>
    public ConcurrencyLimitConfigurationObserver(IConsumePipeConfigurator configurator, int concurrentMessageLimit, string? id = null)
        : base(configurator)
    {
        Limiter = new ConcurrencyLimiter(concurrentMessageLimit, id);

        Connect(this);
    }

    /// <summary>
    /// Gets the limiter value.
    /// </summary>
    public IConcurrencyLimiter Limiter { get; }

    /// <summary>
    /// Performs the message configured operation.
    /// </summary>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public void MessageConfigured<TMessage>(IConsumePipeConfigurator configurator)
        where TMessage : class
    {
        var specification = new ConcurrencyLimitConsumePipeSpecification<TMessage>(Limiter);

        configurator.AddPipeSpecification(specification);
    }
}
