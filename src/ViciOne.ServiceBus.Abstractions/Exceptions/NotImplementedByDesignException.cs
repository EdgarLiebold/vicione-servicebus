namespace ViciOne.ServiceBus
{
    using System;


    [Serializable]
    public class NotImplementedByDesignException :
        ViciOneServiceBusException
    {
        public NotImplementedByDesignException()
            : this("This method has not been implemented by design.")
        {
        }

        public NotImplementedByDesignException(string message)
            : base(message)
        {
        }

        public NotImplementedByDesignException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
