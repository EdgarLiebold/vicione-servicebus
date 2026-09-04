using Xunit;

namespace ViciOne.ServiceBus.Quartz.Tests.QuartzIntegration;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class QuartzIntegrationCollection
{
    public const string Name = "Quartz integration";
}
