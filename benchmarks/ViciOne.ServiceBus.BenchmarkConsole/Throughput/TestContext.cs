// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.BenchmarkConsole.Throughput
{
    using System;


    public interface TestContext :
        PipeContext
    {
        Guid CorrelationId { get; }

        int Attempts { get; set; }
    }
}
