// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Azure.Table.Tests.SlowConcurrentSaga.Events
{
    using System;


    public class IncrementCounterSlowly
    {
        public Guid CorrelationId { get; set; }
    }
}
