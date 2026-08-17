namespace ViciOne.ServiceBus
{
    using System;


    [Serializable]
    public class ValueFactoryException :
        Exception
    {
        public ValueFactoryException()
        {
        }

        public ValueFactoryException(string message)
            : base(message)
        {
        }

        public ValueFactoryException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
