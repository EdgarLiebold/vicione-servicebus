using System;

namespace ViciOne.ServiceBus.BenchmarkConsole.Throughput;

public interface TestContext :
    PipeContext
{
    Guid CorrelationId { get; }

    int Attempts { get; set; }
}
