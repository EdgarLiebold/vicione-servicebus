namespace ViciOne.ServiceBus
{
    using System;


    [Serializable]
    public class MessageInitializerException :
        ViciOneServiceBusException
    {
        public MessageInitializerException()
        {
        }

        public MessageInitializerException(string messageType, string propertyName, string propertType, string message)
            : base($"The {messageType} message initializer for property {propertyName}({propertType}) failed: {message}")
        {
        }

        public MessageInitializerException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
