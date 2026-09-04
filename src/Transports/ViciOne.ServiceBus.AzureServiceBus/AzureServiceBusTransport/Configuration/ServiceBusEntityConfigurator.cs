using System;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>
/// Provides a service bus entity configurator implementation.
/// </summary>
public abstract class ServiceBusEntityConfigurator :
    IServiceBusEntityConfigurator
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    protected ServiceBusEntityConfigurator()
    {
        DefaultMessageTimeToLive = Defaults.DefaultMessageTimeToLive;
    }

    /// <summary>
    /// Gets or sets the auto delete on idle value.
    /// </summary>
    public TimeSpan? AutoDeleteOnIdle { get; set; }

    /// <summary>
    /// Gets or sets the default message time to live value.
    /// </summary>
    public TimeSpan? DefaultMessageTimeToLive { get; set; }

    /// <summary>
    /// Gets or sets the enable batched operations value.
    /// </summary>
    public bool? EnableBatchedOperations { get; set; }

    /// <summary>
    /// Gets or sets the user metadata value.
    /// </summary>
    public string? UserMetadata { get; set; }
}
