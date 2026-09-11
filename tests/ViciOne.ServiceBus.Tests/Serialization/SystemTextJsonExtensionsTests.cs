using System.Text.Json;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class SystemTextJsonExtensionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-OBJECT-CONVERSION", "json-element")]
    public void JsonElement_GetObjectCreatesTheExactRequestedShape()
    {
        using JsonDocument document = JsonDocument.Parse("{\"name\":\"bob\",\"count\":3}");

        CountedMessage? result = document.RootElement.GetObject<CountedMessage>(
            ServiceBusMetadataJson.Options);

        Assert.NotNull(result);
        Assert.Equal("bob", result.Name);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-OBJECT-CONVERSION", "dictionary-transform")]
    public void Dictionary_TransformCreatesTheExactRequestedShape()
    {
        var source = new Dictionary<string, object>
        {
            ["name"] = "bob",
            ["count"] = 3,
        };

        CountedMessage? result = source.Transform<CountedMessage>(ServiceBusMetadataJson.Options);

        Assert.NotNull(result);
        Assert.Equal("bob", result.Name);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-OBJECT-CONVERSION", "interface-with-caller-options")]
    public void JsonElement_GetObjectMaterializesAPublicInterfaceWithCallerOwnedOptions()
    {
        using JsonDocument document = JsonDocument.Parse("{\"name\":\"bob\",\"count\":3}");
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };

        InterfaceValue? result = document.RootElement.GetObject<InterfaceValue>(options);

        Assert.NotNull(result);
        Assert.Equal("bob", result.Name);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-OBJECT-CONVERSION", "runtime-contract-transform")]
    public void RuntimeTransform_CreatesConcreteAndInterfaceContractShapes()
    {
        var source = new { name = "bob", count = 3 };

        object? concrete = source.Transform(typeof(CountedMessage), ServiceBusMetadataJson.Options);
        object? contract = source.Transform(typeof(InterfaceValue), ServiceBusMetadataJson.Options);

        CountedMessage concreteMessage = Assert.IsType<CountedMessage>(concrete);
        Assert.Equal("bob", concreteMessage.Name);
        Assert.Equal(3, concreteMessage.Count);
        InterfaceValue interfaceMessage = Assert.IsAssignableFrom<InterfaceValue>(contract);
        Assert.Equal("bob", interfaceMessage.Name);
        Assert.Equal(3, interfaceMessage.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-OBJECT-CONVERSION", "required-arguments")]
    public void ConversionExtensions_RejectEveryNullRequiredArgument()
    {
        using JsonDocument document = JsonDocument.Parse("{}");
        var source = new object();

        Assert.Equal("options", Assert.Throws<ArgumentNullException>(() =>
            document.RootElement.GetObject<CountedMessage>(null!)).ParamName);
        Assert.Equal("objectToTransform", Assert.Throws<ArgumentNullException>(() =>
            SystemTextJsonExtensions.Transform<CountedMessage>(null!, ServiceBusMetadataJson.Options)).ParamName);
        Assert.Equal("options", Assert.Throws<ArgumentNullException>(() =>
            source.Transform<CountedMessage>(null!)).ParamName);
        Assert.Equal("objectToTransform", Assert.Throws<ArgumentNullException>(() =>
            SystemTextJsonExtensions.Transform(null!, typeof(CountedMessage), ServiceBusMetadataJson.Options)).ParamName);
        Assert.Equal("targetType", Assert.Throws<ArgumentNullException>(() =>
            source.Transform(null!, ServiceBusMetadataJson.Options)).ParamName);
        Assert.Equal("options", Assert.Throws<ArgumentNullException>(() =>
            source.Transform(typeof(CountedMessage), null!)).ParamName);
    }

    public sealed class CountedMessage
    {
        public string? Name { get; init; }

        public int Count { get; init; }
    }

    public interface InterfaceValue
    {
        string? Name { get; }

        int Count { get; }
    }
}
