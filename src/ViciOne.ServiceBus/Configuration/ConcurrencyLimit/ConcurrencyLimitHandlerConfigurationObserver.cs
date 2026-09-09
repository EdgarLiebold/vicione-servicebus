using ViciOne.ServiceBus.Middleware.ConcurrencyLimiting;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Configures a concurrency limit for a handler, on the handler configurator, which is constrained to
/// the message type for that handler, and only applies to the handler.
/// </summary>
internal sealed class ConcurrencyLimitHandlerConfigurationObserver :
    IHandlerConfigurationObserver
{
    /// <summary>Creates an observer that applies one concurrency budget to a handler pipeline.</summary>
    /// <param name="concurrencyLimit">The positive initial limit.</param>
    /// <param name="limiterId">The optional identifier used by management commands.</param>
    public ConcurrencyLimitHandlerConfigurationObserver(int concurrencyLimit, string? limiterId = null)
    {
        Limiter = new ConcurrencyLimiter(concurrencyLimit, limiterId);
    }

    /// <summary>Gets the limiter applied to the handler pipeline.</summary>
    public IConcurrencyLimiter Limiter { get; }

    /// <summary>Adds the limiter to the configured handler pipeline.</summary>
    /// <typeparam name="T">The handled message type.</typeparam>
    /// <param name="configurator">The handler pipeline to limit.</param>
    public void HandlerConfigured<T>(IHandlerConfigurator<T> configurator)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(configurator);

        var specification = new ConcurrencyLimitConsumePipeSpecification<T>(Limiter);

        configurator.AddPipeSpecification(specification);
    }
}
