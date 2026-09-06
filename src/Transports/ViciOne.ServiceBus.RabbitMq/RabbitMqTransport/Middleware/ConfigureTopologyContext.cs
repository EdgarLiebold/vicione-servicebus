namespace ViciOne.ServiceBus.RabbitMq.Middleware;

/// <summary>Identifies one-time topology setup for a specific transport-settings type.</summary>
/// <typeparam name="T">The transport-settings payload associated with the setup.</typeparam>
public interface ConfigureTopologyContext<T>
    where T : class
{
}
