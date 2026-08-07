// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.KafkaIntegration
{
    using System;
    using System.Threading.Tasks;
    using Serialization;


    public class KafkaConsumeContext<TKey, TValue> :
        BodyConsumeContext
        where TValue : class
    {
        public KafkaConsumeContext(ReceiveContext receiveContext, SerializerContext serializationContext)
            : base(receiveContext, serializationContext)
        {
        }

        protected override Task GenerateFault<T>(ConsumeContext<T> context, Exception exception)
        {
            return Task.CompletedTask;
        }
    }
}
