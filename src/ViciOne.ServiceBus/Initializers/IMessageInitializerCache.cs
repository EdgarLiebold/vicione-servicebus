using System;

namespace ViciOne.ServiceBus.Initializers;

public interface IMessageInitializerCache<TMessage>
    where TMessage : class
{
    IMessageInitializer<TMessage> GetInitializer(Type objectType);
}
