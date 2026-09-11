using Xunit;

namespace ViciOne.ServiceBus.Tests.Consumers;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ConsumerConventionStateCollection
{
    public const string Name = "Consumer convention state";
}
