using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.BenchmarkConsole;

[MemoryDiagnoser]
public class SupervisorBenchmark
{
    [Benchmark]
    public async Task AddAgentAndStopAsync()
    {
        var supervisor = new Supervisor();

        var provocateur = new Agent();

        provocateur.SetReady();
        supervisor.SetReady();

        supervisor.Add(provocateur);

        await supervisor.Ready;

        await supervisor.StopAsync();

        await supervisor.Completed;
    }

    [Benchmark]
    public async Task AddAgentWithManagerAndStopAsync()
    {
        var supervisor = new Supervisor();

        var manager = new Supervisor();
        supervisor.Add(manager);

        var provocateur = new Agent();
        manager.Add(provocateur);

        manager.SetReady();
        supervisor.SetReady();
        provocateur.SetReady();

        await supervisor.Ready;

        await supervisor.StopAsync();

        await supervisor.Completed;
    }
}
