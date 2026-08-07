// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using Transports;


    public class MessageNameFormatterEntityNameFormatter :
        IEntityNameFormatter
    {
        readonly IMessageNameFormatter _formatter;

        public MessageNameFormatterEntityNameFormatter(IMessageNameFormatter formatter)
        {
            _formatter = formatter;
        }

        string IEntityNameFormatter.FormatEntityName<T>()
        {
            return _formatter.GetMessageName(typeof(T));
        }
    }
}
