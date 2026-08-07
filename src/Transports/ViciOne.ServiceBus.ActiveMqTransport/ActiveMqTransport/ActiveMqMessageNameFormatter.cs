// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.ActiveMqTransport
{
    using System;
    using Transports;


    public class ActiveMqMessageNameFormatter :
        IMessageNameFormatter
    {
        readonly IMessageNameFormatter _formatter;

        public ActiveMqMessageNameFormatter()
            : this(true)
        {
        }

        public ActiveMqMessageNameFormatter(bool includeNamespace)
        {
            _formatter = new DefaultMessageNameFormatter("::", "--", ".", "-", includeNamespace);
        }

        public string GetMessageName(Type type)
        {
            return _formatter.GetMessageName(type);
        }
    }
}
