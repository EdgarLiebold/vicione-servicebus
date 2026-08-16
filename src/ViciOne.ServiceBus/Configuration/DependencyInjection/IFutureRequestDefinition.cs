namespace ViciOne.ServiceBus
{
    using System;


    public interface IFutureRequestDefinition<TRequest>
        where TRequest : class
    {
        Uri RequestAddress { get; }
    }
}
