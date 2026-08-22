using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Contexts;

/// <summary>
/// The census of the <see cref="MessageBody" /> contract inside the abstractions assembly.
/// </summary>
/// <remarks>
/// Every other Fact of this cohort states something about one named body. None of them notices a
/// sixth implementation appearing beside them, and a body nobody constructs is a body nobody holds
/// to the contract. This class is the one that fails when that happens.
/// <para>
/// The scope is the abstractions assembly alone, which is the assembly this cohort owns. The core
/// and MessagePack bodies are declared elsewhere, still carry inherited obligations, and belong to
/// their own later cohorts; claiming them here would be a completeness claim this project cannot
/// keep.
/// </para>
/// </remarks>
public sealed class MessageBodyContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-BODY-CONTRACT", "abstractions-concrete-type-set")]
    public void EveryConcreteAbstractionsMessageBody_IsInTheExplicitContractSet()
    {
        // Compile-verified type anchors rather than a written list of names. A name list keeps
        // asserting after the thing it names was renamed, moved or dropped, and stays green because
        // it only ever compares itself; if one of these five types stops existing, this file stops
        // compiling.
        var contract = new[]
        {
            IdentityOf(typeof(ArrayMessageBody)),
            IdentityOf(typeof(Base64MessageBody)),
            IdentityOf(typeof(BytesMessageBody)),
            IdentityOf(typeof(EmptyMessageBody)),
            IdentityOf(typeof(StringMessageBody)),
        }.Order(StringComparer.Ordinal);

        // Non-public types are included on purpose: the claim is about what the assembly declares,
        // not about what it exports. An internal body would carry the same contract and would be
        // just as untested. Value types are included for the same reason - filtering on IsClass would
        // have made a struct implementation invisible to a census that claims to see everything.
        var declared = typeof(MessageBody).Assembly.GetTypes()
            .Where(type => !type.IsInterface && !type.IsAbstract && typeof(MessageBody).IsAssignableFrom(type))
            .Select(IdentityOf)
            .Order(StringComparer.Ordinal);

        Assert.Equal(contract, declared);
    }

    private static string IdentityOf(Type type) =>
        type.FullName ?? throw new InvalidOperationException($"Type '{type.Name}' has no full name.");
}
