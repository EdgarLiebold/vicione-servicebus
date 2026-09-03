using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Contracts;

public sealed class MessageContractCatalogTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-V5-CONTRACT-CATALOG", "explicit-and-attribute-bidirectional-lookup")]
    public void Build_ProducesAnImmutableBidirectionalCatalog()
    {
        MessageContractIdentity explicitIdentity = new("vicione.contract.explicit", 2);
        MessageContractIdentity attributedIdentity = new("vicione.contract.attributed", 4);
        MessageContractCatalogBuilder builder = new MessageContractCatalogBuilder()
            .Register<ExplicitContract>(explicitIdentity.Name, explicitIdentity.MajorVersion)
            .Register<AttributedContract>();

        IMessageContractCatalog catalog = builder.Build();

        Assert.Equal(explicitIdentity, catalog.GetIdentity(typeof(ExplicitContract)));
        Assert.Equal(typeof(ExplicitContract), catalog.GetMessageType(explicitIdentity));
        Assert.True(catalog.TryGetIdentity(typeof(AttributedContract), out MessageContractIdentity actualIdentity));
        Assert.Equal(attributedIdentity, actualIdentity);
        Assert.True(catalog.TryGetMessageType(attributedIdentity, out Type? actualType));
        Assert.Equal(typeof(AttributedContract), actualType);
        Assert.Throws<InvalidOperationException>(() => builder.Register<OtherContract>("vicione.contract.other"));
        Assert.Throws<InvalidOperationException>(builder.Build);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-V5-CONTRACT-CATALOG", "idempotent-registration-and-conflict-rejection")]
    public void Registration_IsIdempotentOnlyForTheExactTypeIdentityPair()
    {
        var identity = new MessageContractIdentity("vicione.contract.first", 1);
        var builder = new MessageContractCatalogBuilder();

        Assert.Same(builder, builder.Register(typeof(ExplicitContract), identity));
        Assert.Same(builder, builder.Register(typeof(ExplicitContract), identity));

        ConfigurationException typeConflict = Assert.Throws<ConfigurationException>(() =>
            builder.Register(typeof(ExplicitContract), new MessageContractIdentity("vicione.contract.changed", 1)));
        ConfigurationException identityConflict = Assert.Throws<ConfigurationException>(() =>
            builder.Register(typeof(OtherContract), identity));

        Assert.Contains(typeof(ExplicitContract).FullName!, typeConflict.Message, StringComparison.Ordinal);
        Assert.Contains(identity.ToString(), identityConflict.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-V5-CONTRACT-CATALOG", "invalid-contract-types-rejected")]
    public void Registration_RejectsValueOpenGenericAndUnattributedTypesBeforeMutation()
    {
        var builder = new MessageContractCatalogBuilder();

        Assert.Equal("messageType", Assert.Throws<ArgumentException>(() =>
            builder.Register(typeof(int), new MessageContractIdentity("vicione.contract.value", 1))).ParamName);
        Assert.Equal("messageType", Assert.Throws<ArgumentException>(() =>
            builder.Register(typeof(OpenContract<>), new MessageContractIdentity("vicione.contract.open", 1))).ParamName);
        ConfigurationException missingAttribute = Assert.Throws<ConfigurationException>(() =>
            builder.Register(typeof(ExplicitContract)));
        Assert.Contains(nameof(MessageContractAttribute), missingAttribute.Message, StringComparison.Ordinal);

        IMessageContractCatalog catalog = builder.Register<ExplicitContract>("vicione.contract.valid").Build();
        Assert.Equal(new MessageContractIdentity("vicione.contract.valid", 1), catalog.GetIdentity(typeof(ExplicitContract)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-V5-CONTRACT-CATALOG", "attribute-does-not-flow-through-inheritance")]
    public void AttributeRegistration_RequiresTheConcreteTypeToDeclareItsOwnIdentity()
    {
        MessageContractCatalogBuilder builder = new();

        ConfigurationException exception = Assert.Throws<ConfigurationException>(() =>
            builder.Register(typeof(DerivedAttributedContract)));

        Assert.Contains(typeof(DerivedAttributedContract).FullName!, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-V5-CONTRACT-CATALOG", "unknown-and-null-lookups-fail-closed")]
    public void Catalog_FailsClosedForUnknownOrNullRuntimeLookups()
    {
        IMessageContractCatalog catalog = new MessageContractCatalogBuilder()
            .Register<ExplicitContract>("vicione.contract.explicit")
            .Build();
        var unknown = new MessageContractIdentity("vicione.contract.unknown", 1);

        Assert.False(catalog.TryGetIdentity(typeof(OtherContract), out MessageContractIdentity absentIdentity));
        Assert.Equal(default, absentIdentity);
        Assert.False(catalog.TryGetMessageType(unknown, out Type? absentType));
        Assert.Null(absentType);
        Assert.Throws<MessageContractException>(() => catalog.GetIdentity(typeof(OtherContract)));
        Assert.Throws<MessageContractException>(() => catalog.GetMessageType(unknown));
        Assert.Equal("messageType", Assert.Throws<ArgumentNullException>(() => catalog.GetIdentity(null!)).ParamName);
        Assert.Equal("messageType", Assert.Throws<ArgumentNullException>(() => catalog.TryGetIdentity(null!, out _)).ParamName);
    }

    [MessageContract("vicione.contract.attributed", 4)]
    private sealed class AttributedContract;

    [MessageContract("vicione.contract.base")]
    private class BaseAttributedContract;

    private sealed class DerivedAttributedContract : BaseAttributedContract;
    private sealed class ExplicitContract;
    private sealed class OtherContract;
    private sealed class OpenContract<T>;
}
