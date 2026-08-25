namespace ViciOne.ServiceBus.Context
{
    using System.Collections.Generic;


    public interface TransportSendContext :
        PublishContext
    {
        /// <summary>
        /// Gets the serialized body used by the transport. The value is created once and shared by
        /// transport-adjacent features so serialization is never repeated with a divergent result.
        /// </summary>
        MessageBody Body { get; }

        void WritePropertiesTo(IDictionary<string, object> properties);

        void ReadPropertiesFrom(IReadOnlyDictionary<string, object> properties);
    }


    public interface TransportSendContext<out TMessage> :
        PublishContext<TMessage>,
        TransportSendContext
        where TMessage : class
    {
    }
}
