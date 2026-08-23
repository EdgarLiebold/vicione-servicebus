using Xunit;

namespace ViciOne.ServiceBus.Tests.Testing;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SystemTextJsonGlobalOptionsCollection
{
    public const string Name = "System.Text.Json global options";
}
