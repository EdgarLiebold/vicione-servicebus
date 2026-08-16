namespace ViciOne.ServiceBus
{
    using System;


    public delegate Uri RequestAddressProvider<in TMessage>(BehaviorContext<FutureState, TMessage> context)
        where TMessage : class;
}
