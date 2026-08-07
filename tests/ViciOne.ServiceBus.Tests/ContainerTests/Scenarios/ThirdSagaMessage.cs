// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Tests.ContainerTests.Scenarios
{
    using System;


    public class ThirdSagaMessage
    {
        public Guid CorrelationId { get; set; }
    }
}
