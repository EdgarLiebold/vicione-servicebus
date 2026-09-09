using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Tests.Outbox;

public sealed class OutboxMessageTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-METADATA", "unmaterialized-properties-are-safe-and-empty")]
    public void Properties_BeforeDeserialization_AreEmptyAndImmutable()
    {
        OutboxMessageContext context = new OutboxMessage();

        IReadOnlyDictionary<string, object> properties = context.Properties;

        Assert.Empty(properties);
        IDictionary<string, object> dictionary = Assert.IsAssignableFrom<IDictionary<string, object>>(properties);
        Assert.True(dictionary.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => dictionary.Add("mutable", true));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-METADATA", "deserializer-is-required")]
    public void Deserialize_RejectsANullDeserializer()
    {
        var message = new OutboxMessage();

        Assert.Throws<ArgumentNullException>(() => message.Deserialize(null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-METADATA", "deserialized-properties-are-case-insensitive-and-immutable")]
    public void Deserialize_MaterializesCaseInsensitiveImmutableProperties()
    {
        var message = new OutboxMessage
        {
            Properties = "{\"PartitionKey\":\"north\"}",
        };

        message.Deserialize(ServiceBusMetadataJson.ObjectDeserializer);
        IReadOnlyDictionary<string, object> properties = ((OutboxMessageContext)message).Properties;

        Assert.Equal("north", properties["partitionkey"].ToString());
        IDictionary<string, object> dictionary = Assert.IsAssignableFrom<IDictionary<string, object>>(properties);
        Assert.True(dictionary.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => dictionary.Add("mutable", true));
    }
}
