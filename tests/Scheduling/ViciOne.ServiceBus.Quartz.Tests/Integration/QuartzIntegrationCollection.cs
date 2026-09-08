using Xunit;

namespace ViciOne.ServiceBus.Quartz.Tests.Integration;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class QuartzIntegrationCollection
{
    public const string Name = "Quartz integration";
}
