namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures latest.</summary>
/// <typeparam name="T">The value type.</typeparam>
public interface ILatestConfigurator<T>
    where T : class, PipeContext
{
    /// <summary>Gets or sets the created.</summary>
    LatestFilterCreated<T> Created { set; }
}
