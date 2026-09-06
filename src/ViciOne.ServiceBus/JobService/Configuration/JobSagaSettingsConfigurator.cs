using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures job saga settings.</summary>
public interface JobSagaSettingsConfigurator :
    JobSagaSettings
{
    /// <summary>Gets or sets the job attempt saga endpoint address.</summary>
    new Uri JobAttemptSagaEndpointAddress { set; }
    /// <summary>Gets or sets the job saga endpoint address.</summary>
    new Uri JobSagaEndpointAddress { set; }
    /// <summary>Gets or sets the job type saga endpoint address.</summary>
    new Uri JobTypeSagaEndpointAddress { set; }
}
