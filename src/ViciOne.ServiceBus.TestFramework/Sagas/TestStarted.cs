// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.TestFramework.Sagas
{
    using System;


    public class TestStarted :
        CorrelatedBy<Guid>
    {
        public string TestKey { get; set; }
        public Guid CorrelationId { get; set; }
    }
}
