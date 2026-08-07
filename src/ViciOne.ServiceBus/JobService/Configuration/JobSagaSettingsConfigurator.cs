// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    using System;


    public interface JobSagaSettingsConfigurator :
        JobSagaSettings
    {
        new Uri JobAttemptSagaEndpointAddress { set; }
        new Uri JobSagaEndpointAddress { set; }
        new Uri JobTypeSagaEndpointAddress { set; }
    }
}
