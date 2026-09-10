namespace ViciOne.ServiceBus.AzureServiceBus.Testing;

/// <summary>Controls startup preparation performed by the Azure Service Bus test harness.</summary>
public sealed class AzureServiceBusTestHarnessOptions
{
    /// <summary>Gets or sets whether startup deletes every topic and queue from the configured namespace.</summary>
    public bool CleanNamespaceOnStart { get; set; }
}
