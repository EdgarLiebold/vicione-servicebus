using ViciOne.ServiceBus.Advanced.Topology;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Topology;

public sealed class EntityNameFormatterTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-ENTITY-NAME", "attribute-and-fault-attribute-precedence")]
    public void MessageFormatter_PrefersMessageAndFaultAttributesWithoutCallingFallback()
    {
        var fallback = new ThrowingEntityNameFormatter();

        Assert.Equal("named-message", new MessageEntityNameFormatter<NamedMessage>(fallback).FormatEntityName());
        Assert.Equal("named-fault", new MessageEntityNameFormatter<Fault<FaultNamedMessage>>(fallback).FormatEntityName());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-ENTITY-NAME", "prefix-and-message-name-adapters")]
    public void AdapterFormatters_ReturnExactNamesAndRejectInvalidCollaboratorResults()
    {
        var prefix = new PrefixEntityNameFormatter(new ConstantEntityNameFormatter("orders"), "tenant-");
        Assert.Equal("tenant-orders", prefix.FormatEntityName<Message>());

        IEntityNameFormatter transport = new MessageNameFormatterEntityNameFormatter(new ConstantMessageNameFormatter("contract"));
        Assert.Equal("contract", transport.FormatEntityName<Message>());

        Assert.Equal("entityNameFormatter", Assert.ThrowsAny<ArgumentException>(
            () => new PrefixEntityNameFormatter(new ConstantEntityNameFormatter(" "), "tenant-")
                .FormatEntityName<Message>()).ParamName);
        Assert.Equal("formatter", Assert.ThrowsAny<ArgumentException>(
            () => ((IEntityNameFormatter)new MessageNameFormatterEntityNameFormatter(
                    new ConstantMessageNameFormatter(" ")))
                .FormatEntityName<Message>()).ParamName);
        Assert.Equal("entityNameFormatter", Assert.ThrowsAny<ArgumentException>(
            () => new MessageEntityNameFormatter<Message>(new ConstantEntityNameFormatter(" "))
                .FormatEntityName()).ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-ENTITY-NAME", "nonempty-prefix-and-attribute-values")]
    public void PrefixAndNameAttributes_RejectEveryMissingValue(string? value)
    {
        Assert.Equal("prefix", Assert.ThrowsAny<ArgumentException>(
            () => new PrefixEntityNameFormatter(new ConstantEntityNameFormatter("orders"), value!)).ParamName);
        Assert.Equal("entityName", Assert.ThrowsAny<ArgumentException>(
            () => new EntityNameAttribute(value!)).ParamName);
        Assert.Equal("entityName", Assert.ThrowsAny<ArgumentException>(
            () => new FaultEntityNameAttribute(value!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-ENTITY-NAME", "closed-concrete-formatters")]
    public void ConcreteEntityNameFormatters_AreClosedForInheritance()
    {
        Assert.True(typeof(MessageEntityNameFormatter<>).IsSealed);
        Assert.True(typeof(MessageNameFormatterEntityNameFormatter).IsSealed);
        Assert.True(typeof(MessageUrnEntityNameFormatter).IsSealed);
        Assert.True(typeof(PrefixEntityNameFormatter).IsSealed);
        Assert.True(typeof(StaticEntityNameFormatter<>).IsSealed);

        Assert.Equal(MessageUrn.ForTypeString<Message>(), new MessageUrnEntityNameFormatter().FormatEntityName<Message>());
    }

    private sealed record Message;

    [EntityName("named-message")]
    private sealed record NamedMessage;

    [FaultEntityName("named-fault")]
    private sealed record FaultNamedMessage;

    private sealed class ConstantEntityNameFormatter(string value) : IEntityNameFormatter
    {
        public string FormatEntityName<T>() => value;
    }

    private sealed class ThrowingEntityNameFormatter : IEntityNameFormatter
    {
        public string FormatEntityName<T>() => throw new InvalidOperationException("The attribute must bypass fallback formatting.");
    }

    private sealed class ConstantMessageNameFormatter(string value) : IMessageNameFormatter
    {
        public string GetMessageName(Type type) => value;
    }
}
