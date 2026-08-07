// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.MartenIntegration.Tests
{
    using System;
    using Marten.Schema;


    public class TestSaga : ISaga
    {
        public Guid Id => CorrelationId;

        [Identity]
        public Guid CorrelationId { get; set; }
    }
}
