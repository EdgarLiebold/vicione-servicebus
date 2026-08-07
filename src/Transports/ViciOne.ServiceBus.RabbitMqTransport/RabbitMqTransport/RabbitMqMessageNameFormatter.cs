// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.RabbitMqTransport
{
    using System;
    using Transports;


    public class RabbitMqMessageNameFormatter :
        IMessageNameFormatter
    {
        readonly IMessageNameFormatter _formatter;

        public RabbitMqMessageNameFormatter()
            : this(true)
        {
        }

        public RabbitMqMessageNameFormatter(bool includeNamespace)
        {
            _formatter = new DefaultMessageNameFormatter("::", "--", ":", "-", includeNamespace);
        }

        public string GetMessageName(Type type)
        {
            return _formatter.GetMessageName(type);
        }
    }
}
