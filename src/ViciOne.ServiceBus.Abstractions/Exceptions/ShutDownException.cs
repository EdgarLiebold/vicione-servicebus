// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;


    [Serializable]
    public class ShutDownException :
        ViciOneServiceBusException
    {
        public ShutDownException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
