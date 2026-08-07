// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.MessageData.Conventions
{
    using Initializers;
    using ViciOne.ServiceBus.Configuration;
    using Middleware;


    public class MessageDataMessageSendTopology<T> :
        IMessageSendTopology<T>
        where T : class
    {
        readonly TransformFilter<T> _transformFilter;

        public MessageDataMessageSendTopology(IMessageInitializer<T> initializer)
        {
            _transformFilter = new TransformFilter<T>(initializer);
        }

        public void Apply(ITopologyPipeBuilder<SendContext<T>> builder)
        {
            if (builder.IsImplemented)
                return;

            builder.AddFilter(_transformFilter);
        }
    }
}
