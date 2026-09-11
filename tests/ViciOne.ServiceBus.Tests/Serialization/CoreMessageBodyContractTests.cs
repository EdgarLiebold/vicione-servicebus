using ViciOne.ServiceBus.Mediator;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class CoreMessageBodyContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-BODY-CONTRACT", "core-concrete-type-set")]
    public void EveryConcreteCoreMessageBody_IsInTheExplicitContractSet()
    {
        string[] expected =
        [
            IdentityOf(typeof(MemoryMessageBody)),
            IdentityOf(typeof(SystemTextJsonMessageBody<>)),
            IdentityOf(typeof(SystemTextJsonObjectMessageBody)),
            IdentityOf(typeof(SystemTextJsonRawMessageBody<>)),
        ];
        string[] actual = typeof(MemoryMessageBody).Assembly.GetTypes()
            .Where(type => !type.IsInterface && !type.IsAbstract && typeof(MessageBody).IsAssignableFrom(type))
            .Select(Normalize)
            .Distinct()
            .Select(IdentityOf)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expected.Order(StringComparer.Ordinal), actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-BODY-CONTRACT", "mediator-concrete-type-set")]
    public void EveryConcreteMediatorMessageBody_IsInTheExplicitContractSet()
    {
        string[] actual = typeof(IMediator).Assembly.GetTypes()
            .Where(type => !type.IsInterface && !type.IsAbstract && typeof(MessageBody).IsAssignableFrom(type))
            .Select(Normalize)
            .Distinct()
            .Select(IdentityOf)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["ViciOne.ServiceBus.Mediator.Contexts.MeasuredMediatorMessageBody"], actual);
    }

    private static Type Normalize(Type type) =>
        type.IsGenericType && !type.IsGenericTypeDefinition
            ? type.GetGenericTypeDefinition()
            : type;

    private static string IdentityOf(Type type) =>
        type.FullName ?? throw new InvalidOperationException($"Type '{type.Name}' has no full name.");
}
