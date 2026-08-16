namespace ViciOne.ServiceBus
{
    using System;


    [Serializable]
    public class JobNotFoundException :
        ViciOneServiceBusException
    {
        public JobNotFoundException()
        {
        }

        public JobNotFoundException(string message)
            : base(message)
        {
        }
    }
}
