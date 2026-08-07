// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    public class StaticEntityNameFormatter<TMessage> :
        IMessageEntityNameFormatter<TMessage>
        where TMessage : class
    {
        readonly string _entityName;

        public StaticEntityNameFormatter(string entityName)
        {
            _entityName = entityName;
        }

        string IMessageEntityNameFormatter<TMessage>.FormatEntityName()
        {
            return _entityName;
        }
    }
}
