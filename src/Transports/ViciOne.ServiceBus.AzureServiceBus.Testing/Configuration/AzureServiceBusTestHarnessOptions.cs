namespace ViciOne.ServiceBus.AzureServiceBus.Testing;

/// <summary>
/// Defines configuration options for azure service bus test harness.
/// </summary>
public class AzureServiceBusTestHarnessOptions
{
    /// <summary>
    /// Remove all topics, queues, and subscriptions from the service bus namespace when starting the test harness (via a hosted service)
    /// </summary>
    public bool CleanNamespace { get; set; }
}
