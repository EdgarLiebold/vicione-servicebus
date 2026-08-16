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
