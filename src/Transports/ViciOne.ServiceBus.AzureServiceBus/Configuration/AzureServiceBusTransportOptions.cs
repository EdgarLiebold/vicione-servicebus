namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Configures the Azure Service Bus connection used by dependency-injection registration.</summary>
public sealed class AzureServiceBusTransportOptions
{
    /// <summary>Gets or sets the Azure Service Bus namespace connection string.</summary>
    public string? ConnectionString { get; set; }
}
