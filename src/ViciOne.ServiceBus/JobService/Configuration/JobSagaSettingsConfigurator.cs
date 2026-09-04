using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for job saga settings configurator.
/// </summary>
public interface JobSagaSettingsConfigurator :
    JobSagaSettings
{
    /// <summary>
    /// Gets or sets the job attempt saga endpoint address value.
    /// </summary>
    new Uri JobAttemptSagaEndpointAddress { set; }
    /// <summary>
    /// Gets or sets the job saga endpoint address value.
    /// </summary>
    new Uri JobSagaEndpointAddress { set; }
    /// <summary>
    /// Gets or sets the job type saga endpoint address value.
    /// </summary>
    new Uri JobTypeSagaEndpointAddress { set; }
}
