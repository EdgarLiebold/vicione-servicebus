// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    public class SetCorrelationIdSelector<T> :
        ICorrelationIdSelector<T>
        where T : class
    {
        readonly IMessageCorrelationId<T> _messageCorrelationId;

        public SetCorrelationIdSelector(IMessageCorrelationId<T> messageCorrelationId)
        {
            _messageCorrelationId = messageCorrelationId;
        }

        public bool TryGetSetCorrelationId(out IMessageCorrelationId<T> messageCorrelationId)
        {
            messageCorrelationId = _messageCorrelationId;
            return true;
        }
    }
}
