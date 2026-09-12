using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.MessagePack.Serialization;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.MessagePack.Tests.Serialization;

public sealed class MessagePackObjectSerializationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-OBJECT-SERIALIZATION", "null-and-value-bodies")]
    public void SerializeObject_ProducesAnEmptyBodyForNullAndMessagePackForAValue()
    {
        var serializer = new MessagePackMessageSerializer();

        MessageBody empty = serializer.SerializeObject(null);
        MessageBody body = serializer.SerializeObject(new ObjectValue { Id = 27, Name = "Frank" });
        var restored = MessagePackSerializationRuntime.Deserialize<ObjectValue>(body.ToArray());

        Assert.IsType<EmptyMessageBody>(empty);
        Assert.Empty(empty.ToArray());
        Assert.NotEmpty(body.ToArray());
        Assert.Equal(27, restored.Id);
        Assert.Equal("Frank", restored.Name);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-OBJECT-SERIALIZATION", "reference-input-forms")]
    public void DeserializeObject_HandlesEveryReferenceInputForm()
    {
        var serializer = new MessagePackMessageSerializer();
        var fallback = new ObjectValue { Id = -1, Name = "fallback" };
        var direct = new ObjectValue { Id = 1, Name = "direct" };
        byte[] bytes = MessagePackSerializationRuntime.Serialize(
            new ObjectValue { Id = 2, Name = "bytes" });

        ObjectValue? fromMissing = serializer.DeserializeObject<ObjectValue>(null, fallback);
        ObjectValue? fromDirect = serializer.DeserializeObject<ObjectValue>(direct, fallback);
        ObjectValue? fromDictionary = serializer.DeserializeObject<ObjectValue>(
            new Dictionary<string, object>
            {
                ["ID"] = 3,
                ["name"] = "dictionary",
            });
        ObjectValue? fromBytes = serializer.DeserializeObject<ObjectValue>(bytes);
        string json = System.Text.Json.JsonSerializer.Serialize(
            new ObjectValue { Id = 4, Name = "json" },
            ServiceBusMetadataJson.Options);
        ObjectValue? fromJson = serializer.DeserializeObject<ObjectValue>(json);

        Assert.Same(fallback, fromMissing);
        Assert.Same(direct, fromDirect);
        Assert.Equal(3, fromDictionary!.Id);
        Assert.Equal("dictionary", fromDictionary.Name);
        Assert.Equal(2, fromBytes!.Id);
        Assert.Equal("bytes", fromBytes.Name);
        Assert.Equal(4, fromJson!.Id);
        Assert.Equal("json", fromJson.Name);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-OBJECT-SERIALIZATION", "value-input-forms")]
    public void DeserializeObject_HandlesEveryValueInputForm()
    {
        var serializer = new MessagePackMessageSerializer();
        byte[] bytes = MessagePackSerializationRuntime.Serialize(29);

        int? fromMissing = serializer.DeserializeObject<int>(null, 23);
        int? fromDirect = serializer.DeserializeObject<int>(27, 23);
        int? fromText = serializer.DeserializeObject<int>("28", 23);
        int? fromBytes = serializer.DeserializeObject<int>(bytes, 23);

        Assert.Equal(23, fromMissing);
        Assert.Equal(27, fromDirect);
        Assert.Equal(28, fromText);
        Assert.Equal(29, fromBytes);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-OBJECT-SERIALIZATION", "blank-text-is-absent")]
    public void DeserializeObject_TreatsBlankTextAsAnAbsentValue()
    {
        var serializer = new MessagePackMessageSerializer();
        var fallback = new ObjectValue { Id = -1, Name = "fallback" };

        ObjectValue? reference = serializer.DeserializeObject<ObjectValue>(" \r\n\t", fallback);
        ObjectValue? nullReference = serializer.DeserializeObject<ObjectValue>("null", fallback);
        int? value = serializer.DeserializeObject<int>(" \r\n\t", 23);
        int? nullValue = serializer.DeserializeObject<int>("null", 23);

        Assert.Same(fallback, reference);
        Assert.Same(fallback, nullReference);
        Assert.Equal(23, value);
        Assert.Equal(23, nullValue);
    }

    private sealed class ObjectValue
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}
