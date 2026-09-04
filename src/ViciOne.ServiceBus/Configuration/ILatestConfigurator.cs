namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for latest configurator.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public interface ILatestConfigurator<T>
    where T : class, PipeContext
{
    /// <summary>
    /// Gets or sets the created value.
    /// </summary>
    LatestFilterCreated<T> Created { set; }
}
