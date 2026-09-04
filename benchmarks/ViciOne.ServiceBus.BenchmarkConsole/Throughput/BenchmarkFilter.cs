using System.Threading.Tasks;

namespace ViciOne.ServiceBus.BenchmarkConsole.Throughput;

public class BenchmarkFilter :
    IFilter<TestContext>
{
    public Task SendAsync(TestContext context, IPipe<TestContext> next)
    {
        return next.SendAsync(context);
    }

    public void Probe(ProbeContext context)
    {
    }
}
