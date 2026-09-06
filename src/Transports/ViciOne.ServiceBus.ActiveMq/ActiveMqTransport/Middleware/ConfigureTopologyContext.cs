namespace ViciOne.ServiceBus.ActiveMq.Middleware;

/// <summary>Keys one-time ActiveMQ topology setup by its associated settings type.</summary>
/// <typeparam name="T">The topology settings type.</typeparam>
public interface ConfigureTopologyContext<T>
    where T : class
{
}
