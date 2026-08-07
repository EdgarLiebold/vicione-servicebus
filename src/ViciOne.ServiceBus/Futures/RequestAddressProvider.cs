// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;


    public delegate Uri RequestAddressProvider<in TMessage>(BehaviorContext<FutureState, TMessage> context)
        where TMessage : class;
}
