namespace ViciOne.ServiceBus
{
    using System;
    using System.Runtime.Serialization;


    [Serializable]
    public sealed class RabbitMqAddressException :
        ConfigurationException
    {
        const string DefaultHelpLink = "http://www.rabbitmq.com/specification.html";

        public RabbitMqAddressException()
        {
            HelpLink = DefaultHelpLink;
        }

        public RabbitMqAddressException(string message)
            : base(message)
        {
            HelpLink = DefaultHelpLink;
        }

        public RabbitMqAddressException(string message, Exception innerException)
            : base(message, innerException)
        {
            HelpLink = DefaultHelpLink;
        }

        [Obsolete("Formatter-based serialization is obsolete and should not be used.")]
        public RabbitMqAddressException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
