namespace ViciOne.ServiceBus.AzureServiceBus.Middleware;

/// <summary>Marks completion of one-time Azure Service Bus topology configuration for a settings instance.</summary>
/// <typeparam name="T">The entity settings associated with the deployment.</typeparam>
public interface ConfigureTopologyContext<T>
    where T : class
{
}
