namespace ViciOne.ServiceBus.AzureServiceBus.Testing;

/// <summary>Configures namespace cleanup for the Azure Service Bus test harness.</summary>
public sealed class AzureServiceBusTestHarnessOptions
{
    /// <summary>Gets or sets whether the hosted service deletes all topics and queues from the configured namespace when it starts.</summary>
    public bool CleanNamespace { get; set; }
}
