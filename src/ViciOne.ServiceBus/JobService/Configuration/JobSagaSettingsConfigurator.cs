using System;

namespace ViciOne.ServiceBus.Configuration;

public interface JobSagaSettingsConfigurator :
    JobSagaSettings
{
    new Uri JobAttemptSagaEndpointAddress { set; }
    new Uri JobSagaEndpointAddress { set; }
    new Uri JobTypeSagaEndpointAddress { set; }
}
