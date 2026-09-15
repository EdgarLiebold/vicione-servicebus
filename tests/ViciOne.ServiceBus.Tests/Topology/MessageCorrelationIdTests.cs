using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Topology;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Topology;

public sealed class MessageCorrelationIdTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-CORRELATION", "required-and-nullable-value-shapes")]
    public void DelegateResolvers_DistinguishMissingEmptyAndNonEmptyIdentifiers()
    {
        Guid expected = Guid.Parse("d1c8ffab-cd9a-42a2-a70e-84288cf4a4d8");
        var required = new DelegateMessageCorrelationId<RequiredMessage>(message => message.CorrelationId);
        var nullable = new NullableDelegateMessageCorrelationId<NullableMessage>(message => message.CorrelationId);

        Assert.False(required.TryGetCorrelationId(new RequiredMessage(Guid.Empty), out Guid emptyRequired));
        Assert.Equal(Guid.Empty, emptyRequired);
        Assert.True(required.TryGetCorrelationId(new RequiredMessage(expected), out Guid requiredValue));
        Assert.Equal(expected, requiredValue);

        Assert.False(nullable.TryGetCorrelationId(new NullableMessage(null), out Guid missingNullable));
        Assert.Equal(Guid.Empty, missingNullable);
        Assert.False(nullable.TryGetCorrelationId(new NullableMessage(Guid.Empty), out Guid emptyNullable));
        Assert.Equal(Guid.Empty, emptyNullable);
        Assert.True(nullable.TryGetCorrelationId(new NullableMessage(expected), out Guid nullableValue));
        Assert.Equal(expected, nullableValue);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-CORRELATION", "required-and-nullable-property-shapes")]
    public void PropertyResolvers_DistinguishMissingEmptyNonEmptyAndUnsupportedProperties()
    {
        Guid expected = Guid.Parse("f8759afb-6653-451f-a37e-cf7a2bc02170");
        var requiredSelector = new PropertyCorrelationIdSelector<RequiredMessage>(nameof(RequiredMessage.CorrelationId));
        var nullableSelector = new PropertyCorrelationIdSelector<NullableMessage>(nameof(NullableMessage.CorrelationId));
        var unsupportedSelector = new PropertyCorrelationIdSelector<UnsupportedMessage>(nameof(UnsupportedMessage.CorrelationId));

        Assert.True(requiredSelector.TryGetCorrelationIdResolver(out IMessageCorrelationId<RequiredMessage>? required));
        Assert.False(required.TryGetCorrelationId(new RequiredMessage(Guid.Empty), out Guid emptyRequired));
        Assert.Equal(Guid.Empty, emptyRequired);
        Assert.True(required.TryGetCorrelationId(new RequiredMessage(expected), out Guid requiredValue));
        Assert.Equal(expected, requiredValue);

        Assert.True(nullableSelector.TryGetCorrelationIdResolver(out IMessageCorrelationId<NullableMessage>? nullable));
        Assert.False(nullable.TryGetCorrelationId(new NullableMessage(null), out Guid missingNullable));
        Assert.Equal(Guid.Empty, missingNullable);
        Assert.False(nullable.TryGetCorrelationId(new NullableMessage(Guid.Empty), out Guid emptyNullable));
        Assert.Equal(Guid.Empty, emptyNullable);
        Assert.True(nullable.TryGetCorrelationId(new NullableMessage(expected), out Guid nullableValue));
        Assert.Equal(expected, nullableValue);

        Assert.False(unsupportedSelector.TryGetCorrelationIdResolver(out IMessageCorrelationId<UnsupportedMessage>? unsupported));
        Assert.Null(unsupported);
    }

    private sealed record RequiredMessage(Guid CorrelationId);

    private sealed record NullableMessage(Guid? CorrelationId);

    private sealed record UnsupportedMessage(string CorrelationId);
}
