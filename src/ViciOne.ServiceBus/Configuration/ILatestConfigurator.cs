namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures latest.</summary>
/// <typeparam name="T">The value type.</typeparam>
public interface ILatestConfigurator<T>
    where T : class, PipeContext
{
    /// <summary>Sets the callback invoked when the latest-value filter is created.</summary>
    LatestFilterCreated<T> Created { set; }
}
