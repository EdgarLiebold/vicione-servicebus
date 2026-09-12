using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.Conventions;
using ViciOne.ServiceBus.Initializers.Factories;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers;

public sealed class MessageInitializerFactoryContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-CONSTRUCTION", "factory-rejects-missing-conventions-and-null-elements")]
    public void Constructor_RejectsMissingConventionsAndNullElements()
    {
        IInitializerConvention[] conventions = [null!];

        Assert.Equal("conventions", Assert.Throws<ArgumentNullException>(() =>
            new MessageInitializerFactory<TestMessage, TestInput>(null!)).ParamName);
        Assert.Equal("conventions", Assert.Throws<ArgumentNullException>(() =>
            new MessageInitializerFactory<TestMessage, TestInput>(messageFactory: null, conventions: null!)).ParamName);
        Assert.Equal("conventions", Assert.Throws<ArgumentException>(() =>
            new MessageInitializerFactory<TestMessage, TestInput>(conventions)).ParamName);
        Assert.Equal("conventions", Assert.Throws<ArgumentException>(() =>
            new MessageInitializerFactory<TestMessage, TestInput>(messageFactory: null, conventions)).ParamName);
    }

    private sealed class TestMessage;

    private sealed class TestInput;
}
