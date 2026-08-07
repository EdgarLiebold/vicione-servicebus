// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;


    public delegate Guid PendingFutureIdProvider<in T>(T message)
        where T : class;
}
