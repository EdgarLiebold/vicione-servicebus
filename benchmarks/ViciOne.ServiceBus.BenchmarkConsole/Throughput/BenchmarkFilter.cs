using System.Threading.Tasks;

namespace ViciOne.ServiceBus.BenchmarkConsole.Throughput;

public class BenchmarkFilter :
    IFilter<TestContext>
{
    public Task Send(TestContext context, IPipe<TestContext> next)
    {
        return next.Send(context);
    }

    public void Probe(ProbeContext context)
    {
    }
}
