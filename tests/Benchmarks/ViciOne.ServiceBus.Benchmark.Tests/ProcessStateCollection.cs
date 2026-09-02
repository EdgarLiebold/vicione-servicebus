using Xunit;

namespace ViciOne.ServiceBus.Benchmark.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ProcessStateCollection
{
    public const string Name = "Benchmark process state";
}
