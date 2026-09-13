using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Receives the endpoint addresses assigned to job-service state machines.</summary>
internal interface IJobSagaSettingsConfigurator :
    IJobSagaSettings
{
    /// <summary>Sets the job-attempt coordination endpoint address.</summary>
    new Uri JobAttemptSagaEndpointAddress { set; }
    /// <summary>Sets the job lifecycle coordination endpoint address.</summary>
    new Uri JobSagaEndpointAddress { set; }
    /// <summary>Sets the job-type capacity coordination endpoint address.</summary>
    new Uri JobTypeSagaEndpointAddress { set; }
}
