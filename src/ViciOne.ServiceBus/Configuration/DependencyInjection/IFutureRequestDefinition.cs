using System;

namespace ViciOne.ServiceBus;

public interface IFutureRequestDefinition<TRequest>
    where TRequest : class
{
    Uri RequestAddress { get; }
}
