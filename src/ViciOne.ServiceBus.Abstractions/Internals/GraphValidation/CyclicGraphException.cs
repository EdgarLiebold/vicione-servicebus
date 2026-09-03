namespace ViciOne.ServiceBus.Internals.GraphValidation
{
    using System;


    [Serializable]
    internal class CyclicGraphException :
        ViciOneServiceBusException
    {
        public CyclicGraphException()
        {
        }

        public CyclicGraphException(string message)
            : base(message)
        {
        }

        public CyclicGraphException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
