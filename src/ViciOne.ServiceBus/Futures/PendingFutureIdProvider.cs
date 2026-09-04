using System;

namespace ViciOne.ServiceBus;

public delegate Guid PendingFutureIdProvider<in T>(T message)
    where T : class;
