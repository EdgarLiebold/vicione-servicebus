using System;

namespace ViciOne.ServiceBus;

public delegate Uri RequestAddressProvider<in TMessage>(BehaviorContext<FutureState, TMessage> context)
    where TMessage : class;
