using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.BenchmarkConsole.Throughput;

public class FaultFilter :
    IFilter<TestContext>
{
    public Task Send(TestContext context, IPipe<TestContext> next)
    {
        throw new InvalidOperationException();
    }

    public void Probe(ProbeContext context)
    {
    }
}
