// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;


    public interface IFutureRequestDefinition<TRequest>
        where TRequest : class
    {
        Uri RequestAddress { get; }
    }
}
