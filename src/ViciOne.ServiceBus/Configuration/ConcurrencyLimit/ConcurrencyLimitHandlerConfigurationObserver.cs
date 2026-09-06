using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Configures a concurrency limit for a handler, on the handler configurator, which is constrained to
/// the message type for that handler, and only applies to the handler.
/// </summary>
public class ConcurrencyLimitHandlerConfigurationObserver :
    IHandlerConfigurationObserver
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="concurrentMessageLimit">The concurrent message limit.</param>
    /// <param name="id">The id.</param>
    public ConcurrencyLimitHandlerConfigurationObserver(int concurrentMessageLimit, string? id = null)
    {
        Limiter = new ConcurrencyLimiter(concurrentMessageLimit, id);
    }

    /// <summary>Gets the limiter.</summary>
    public IConcurrencyLimiter Limiter { get; }

    /// <summary>Reports that handler has been configured.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public void HandlerConfigured<T>(IHandlerConfigurator<T> configurator)
        where T : class
    {
        var specification = new ConcurrencyLimitConsumePipeSpecification<T>(Limiter);

        configurator.AddPipeSpecification(specification);
    }
}
