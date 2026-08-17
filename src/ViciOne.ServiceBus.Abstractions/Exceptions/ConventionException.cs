namespace ViciOne.ServiceBus
{
    using System;


    [Serializable]
    public class ConventionException :
        ViciOneServiceBusException
    {
        public ConventionException()
        {
        }

        public ConventionException(string message)
            : base(message)
        {
        }

        public ConventionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
