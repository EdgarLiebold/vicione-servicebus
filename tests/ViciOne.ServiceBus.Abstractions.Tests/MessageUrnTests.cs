using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests;

public sealed class MessageUrnTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-URN-ATTRIBUTE", "declared-default-urn")]
    public void AttributedType_UsesDeclaredUrn()
    {
        var actual = MessageUrn.ForTypeString<AttributedMessage>();

        Assert.Equal("urn:message:MyCustomName", actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-URN-ATTRIBUTE", "attributed-array")]
    public void AttributedArray_UsesElementUrnWithArraySuffix()
    {
        var actual = MessageUrn.ForTypeString<AttributedMessage[]>();

        Assert.Equal("urn:message:MyCustomName[]", actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-URN-ATTRIBUTE", "unicode-and-punctuation")]
    public void AttributedType_PreservesUnicodeAndPunctuation()
    {
        var actual = MessageUrn.ForType<UnicodePunctuationMessage>();

        Assert.Equal("urn:message:events/Gr\u00FC\u00DFe?percent=100%25", actual.OriginalString);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-URN-ATTRIBUTE", "custom-absolute-uri")]
    public void CustomSchemeAttribute_IsReturnedWithoutDefaultPrefix()
    {
        var actual = MessageUrn.ForTypeString<CustomSchemeMessage>();

        Assert.Equal("scheme:identifier", actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-URN-DERIVATION", "plain-type")]
    public void PlainType_UsesNamespaceAndName()
    {
        var actual = MessageUrn.ForTypeString<PlainMessage>();

        Assert.Equal("urn:message:ViciOne.ServiceBus.Abstractions.Tests:PlainMessage", actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-URN-DERIVATION", "nested-type")]
    public void NestedType_IncludesDeclaringType()
    {
        var actual = MessageUrn.ForTypeString<NestedMessage>();

        Assert.Equal(
            "urn:message:ViciOne.ServiceBus.Abstractions.Tests:MessageUrnTests+NestedMessage",
            actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-URN-DERIVATION", "closed-generic")]
    public void ClosedGenericType_UsesNestedArgumentBrackets()
    {
        var actual = MessageUrn.ForTypeString<GenericEnvelope<PlainMessage>>();

        Assert.Equal(
            "urn:message:ViciOne.ServiceBus.Abstractions.Tests:GenericEnvelope[[ViciOne.ServiceBus.Abstractions.Tests:PlainMessage]]",
            actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-URN-DERIVATION", "invalid-runtime-types-rejected")]
    public void RuntimeTypeOverloads_RejectNullAndOpenGenericTypes()
    {
        var urnException = Assert.Throws<ArgumentException>(
            () => MessageUrn.ForType(typeof(GenericEnvelope<>)));
        var stringException = Assert.Throws<ArgumentException>(
            () => MessageUrn.ForTypeString(typeof(GenericEnvelope<>)));
        var urnNullException = Assert.Throws<ArgumentNullException>(
            () => MessageUrn.ForType(null!));
        var stringNullException = Assert.Throws<ArgumentNullException>(
            () => MessageUrn.ForTypeString(null!));

        Assert.Equal("type", urnException.ParamName);
        Assert.Equal("type", stringException.ParamName);
        Assert.Equal("type", urnNullException.ParamName);
        Assert.Equal("type", stringNullException.ParamName);
    }

    [Theory]
    [InlineData(DeconstructionShape.NameOnly, "MyCustomName", null, null)]
    [InlineData(DeconstructionShape.NamespaceAndName, "OrderSubmitted", "Contracts", null)]
    [InlineData(DeconstructionShape.AssemblyQualified, "OrderSubmitted", "Contracts", "ContractAssembly")]
    [InlineData(DeconstructionShape.NonMessageScheme, null, null, null)]
    [RequirementCoverage("REQ-VSB-MESSAGE-URN-DECONSTRUCTION", "all-supported-shapes")]
    public void Deconstruct_ReturnsExpectedComponents(
        DeconstructionShape shape,
        string? expectedName,
        string? expectedNamespace,
        string? expectedAssembly)
    {
        var urn = shape switch
        {
            DeconstructionShape.NameOnly => MessageUrn.ForType<AttributedMessage>(),
            DeconstructionShape.NamespaceAndName => MessageUrn.ForType<NamespacedAttributedMessage>(),
            DeconstructionShape.AssemblyQualified => MessageUrn.ForType<AssemblyAttributedMessage>(),
            DeconstructionShape.NonMessageScheme => MessageUrn.ForType<CustomSchemeMessage>(),
            _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, null),
        };

        var (name, ns, assemblyName) = urn;

        Assert.Equal(expectedName, name);
        Assert.Equal(expectedNamespace, ns);
        Assert.Equal(expectedAssembly, assemblyName);
    }

    public enum DeconstructionShape
    {
        NameOnly,
        NamespaceAndName,
        AssemblyQualified,
        NonMessageScheme,
    }

    private sealed class NestedMessage
    {
    }
}

[MessageUrn("MyCustomName")]
internal sealed class AttributedMessage
{
}

[MessageUrn("events/Gr\u00FC\u00DFe?percent=100%")]
internal sealed class UnicodePunctuationMessage
{
}

[MessageUrn("scheme:identifier", useDefaultPrefix: false)]
internal sealed class CustomSchemeMessage
{
}

[MessageUrn("Contracts:OrderSubmitted")]
internal sealed class NamespacedAttributedMessage
{
}

[MessageUrn("Contracts:OrderSubmitted:ContractAssembly")]
internal sealed class AssemblyAttributedMessage
{
}

internal sealed class PlainMessage
{
}

internal sealed class GenericEnvelope<T>
    where T : class
{
}
