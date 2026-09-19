using System.Runtime.Serialization;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Tests.Outbox;

public sealed class OutboxMessageTests
{
    private const string AdmissionPrefix = "VOSB-EF-OUTBOX-ADMISSION/1:";
    private static readonly string Digest = new('A', 64);

    public static TheoryData<string> MalformedAdmissionFrames => new()
    {
        AdmissionPrefix,
        AdmissionPrefix + "x:0:" + Digest + "\n{}",
        AdmissionPrefix + "-1:0:" + Digest + "\n{}",
        AdmissionPrefix + "2147483648:0:" + Digest + "\n{}",
        AdmissionPrefix + "1:2:" + Digest + "\n{}",
        AdmissionPrefix + "1;0:" + Digest + "\n{}",
        AdmissionPrefix + "1:0;" + Digest + "\n{}",
        AdmissionPrefix + "1:0:" + new string('A', 63) + "\n{}",
        AdmissionPrefix + "1:0:" + new string('Z', 64) + "\n{}",
        AdmissionPrefix + "1:0:" + Digest + "x{}",
    };

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

    [Theory]
    [MemberData(nameof(MalformedAdmissionFrames))]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-METADATA", "corrupt-admission-proof-fails-closed")]
    public void Deserialize_RejectsMalformedPersistedAdmissionProof(string storedHeaders)
    {
        var message = new OutboxMessage { Headers = storedHeaders };

        SerializationException failure = Assert.Throws<SerializationException>(
            () => message.Deserialize(ServiceBusMetadataJson.ObjectDeserializer));

        Assert.Contains("admission proof is malformed", failure.Message, StringComparison.Ordinal);
        Assert.Empty(((MessageContext)message).Headers.GetAll());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-METADATA", "framed-proof-preserves-application-headers")]
    public void Deserialize_PreservesApplicationHeadersAfterFramedAdmissionProof()
    {
        var message = new OutboxMessage
        {
            Headers = AdmissionPrefix + "17:0:" + Digest + "\n{\"tenant\":\"north\"}",
        };

        message.Deserialize(ServiceBusMetadataJson.ObjectDeserializer);

        Headers headers = ((MessageContext)message).Headers;
        Assert.Equal("north", headers.Get<string>("tenant"));
        Assert.Single(headers.GetAll());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-METADATA", "framed-proof-without-application-headers-stays-empty")]
    public void Deserialize_FramedAdmissionProofDoesNotBecomeAnApplicationHeader()
    {
        var message = new OutboxMessage
        {
            Headers = AdmissionPrefix + "0:0:" + Digest + "\n",
        };

        message.Deserialize(ServiceBusMetadataJson.ObjectDeserializer);

        Assert.Empty(((MessageContext)message).Headers.GetAll());
    }
}
