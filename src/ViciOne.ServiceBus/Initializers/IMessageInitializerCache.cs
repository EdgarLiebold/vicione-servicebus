// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Initializers
{
    using System;


    public interface IMessageInitializerCache<TMessage>
        where TMessage : class
    {
        IMessageInitializer<TMessage> GetInitializer(Type objectType);
    }
}
