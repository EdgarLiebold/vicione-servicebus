namespace ViciOne.ServiceBus.AmazonSqs.Middleware;

/// <summary>Marks one-time topology setup state for a specific entity-settings type.</summary>
/// <typeparam name="T">The entity-settings type.</typeparam>
public interface ConfigureTopologyContext<T>
    where T : class
{
}
