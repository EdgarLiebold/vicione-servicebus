using System.Text;
using RabbitMQ.Client;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.RabbitMqTransport;

public sealed class MoveTransportHeadersTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-MOVE-HEADERS", "overwrite-add-remove-semantics")]
    public void Set_RespectsOverwriteAndRemovalSemantics()
    {
        var properties = new BasicProperties();
        var headers = new MoveTransportHeaders(properties);

        headers.Set("existing", "first");
        headers.Set("existing", "ignored", overwrite: false);
        Assert.Equal("first", properties.Headers!["existing"]);
        headers.Set("existing", null, overwrite: false);
        Assert.Equal("first", properties.Headers["existing"]);
        headers.Set("new", 17, overwrite: false);
        Assert.Equal(17, properties.Headers["new"]);
        headers.Set("missing", null, overwrite: false);
        headers.Set("existing", "replaced", overwrite: true);
        headers.Set("new", null, overwrite: true);

        Assert.NotNull(properties.Headers);
        Assert.Single(properties.Headers);
        Assert.Equal("replaced", properties.Headers["existing"]);
        Assert.False(properties.Headers.ContainsKey("new"));
        Assert.False(properties.Headers.ContainsKey("missing"));
        Assert.Equal("key", Assert.Throws<ArgumentNullException>(() => headers.Set(null!, "value")).ParamName);
        Assert.Equal("key", Assert.Throws<ArgumentNullException>(() => headers.Set(null!, 1, true)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-MOVE-HEADERS", "empty-table-and-string-removal-preserve-neighbors")]
    public void EmptyTableAndStringRemoval_PreserveAbsenceAndUnrelatedValues()
    {
        var properties = new BasicProperties();
        var headers = new MoveTransportHeaders(properties);

        Assert.Null(properties.Headers);
        Assert.False(headers.TryGetHeader("missing", out object? missing));
        Assert.Null(missing);
        Assert.Empty(headers.GetAll());
        Assert.Empty(headers);
        Assert.Empty(((System.Collections.IEnumerable)headers).Cast<HeaderValue>());
        Assert.Null(properties.Headers);

        headers.Set("target", "remove");
        headers.Set("neighbor", false, true);
        headers.Set("target", null);
        headers.Set("missing", null);

        Assert.False(headers.TryGetHeader("target", out object? removed));
        Assert.Null(removed);
        HeaderValue retained = Assert.Single(((System.Collections.IEnumerable)headers).Cast<HeaderValue>());
        Assert.Equal("neighbor", retained.Key);
        Assert.False(Assert.IsType<bool>(retained.Value));
        Assert.Single(properties.Headers!);
        Assert.False(Assert.IsType<bool>(properties.Headers!["neighbor"]));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-MOVE-HEADERS", "point-lookup-normalization")]
    public void TryGetHeader_DecodesBytesAndRejectsNullOrMissingValues()
    {
        var properties = new BasicProperties
        {
            Headers = new Dictionary<string, object?>
            {
                ["bytes"] = Encoding.UTF8.GetBytes("decoded"),
                ["number"] = 0,
                ["null"] = null,
            },
        };
        var headers = new MoveTransportHeaders(properties);

        Assert.True(headers.TryGetHeader("bytes", out object? decoded));
        Assert.Equal("decoded", decoded);
        Assert.True(headers.TryGetHeader("number", out object? number));
        Assert.Equal(0, number);
        Assert.False(headers.TryGetHeader("null", out object? nullValue));
        Assert.Null(nullValue);
        Assert.False(headers.TryGetHeader("absent", out object? absent));
        Assert.Null(absent);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-MOVE-HEADERS", "non-null-wire-enumeration")]
    public void Enumeration_PreservesEveryNonNullWireValue()
    {
        byte[] wireBytes = Encoding.UTF8.GetBytes("decoded");
        var properties = new BasicProperties
        {
            Headers = new Dictionary<string, object?>
            {
                ["bytes"] = wireBytes,
                ["number"] = 0,
                ["null"] = null,
            },
        };
        var headers = new MoveTransportHeaders(properties);

        Dictionary<string, object> pairs = headers.GetAll().ToDictionary(pair => pair.Key, pair => pair.Value);
        Dictionary<string, object> enumerated = headers.ToDictionary(value => value.Key, value => value.Value);

        Assert.Equal(2, pairs.Count);
        Assert.Same(wireBytes, pairs["bytes"]);
        Assert.Equal(0, pairs["number"]);
        Assert.Equal(pairs.Keys.Order(), enumerated.Keys.Order());
        Assert.Same(wireBytes, enumerated["bytes"]);
        Assert.Equal(0, enumerated["number"]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-MOVE-HEADERS", "typed-retrieval-is-unsupported")]
    public void TypedGet_RejectsBothReferenceAndValueRequests()
    {
        var headers = new MoveTransportHeaders(new BasicProperties());

        NotSupportedException reference = Assert.Throws<NotSupportedException>(() => headers.Get<string>("key", null));
        NotSupportedException value = Assert.Throws<NotSupportedException>(() => headers.Get<int>("key", 0));

        Assert.Equal("RabbitMQ move-transport headers do not support object-based retrieval.", reference.Message);
        Assert.Equal(reference.Message, value.Message);
    }
}
