using System.Collections;
using Apache.NMS.Util;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMq.Tests.ActiveMqTransport;

public sealed class PrimitiveMapHeadersTests
{
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-HEADERS", "primitive-map-overwrite-preserves-native-neighbors")]
    public void Set_ChangesOnlyThePermittedNativeProperty(bool overwrite, bool exists, bool remove)
    {
        var map = new PrimitiveMap();
        map["neighbor"] = 73L;
        if (exists)
            map["target"] = "original";
        var headers = new PrimitiveMapHeaders(map);

        headers.Set("target", remove ? null : (object)42, overwrite);

        bool expectedPresent = overwrite ? !remove : exists || !remove;
        Assert.Equal(expectedPresent, map.Contains("target"));
        Assert.Equal(expectedPresent ? 2 : 1, map.Count);
        Assert.Equal(73L, Assert.IsType<long>(map["neighbor"]));
        if (expectedPresent)
        {
            object expected = !overwrite && exists ? "original" : 42;
            Assert.Equal(expected, map["target"]);
        }
        PrimitiveMap wire = PrimitiveMap.Unmarshal(map.Marshal());
        Assert.Equal(expectedPresent, wire.Contains("target"));
        Assert.Equal(73L, Assert.IsType<long>(wire["neighbor"]));
        if (expectedPresent)
            Assert.Equal(!overwrite && exists ? (object)"original" : 42, wire["target"]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-HEADERS", "primitive-map-string-replacement-and-removal")]
    public void SetString_ReplacesAndRemovesWithoutChangingOtherProperties()
    {
        var map = new PrimitiveMap();
        map["neighbor"] = false;
        var headers = new PrimitiveMapHeaders(map);

        headers.Set("target", "first");
        Assert.Equal("first", map["target"]);
        headers.Set("target", "second");
        Assert.Equal("second", map["target"]);
        headers.Set("target", null);
        headers.Set("absent", null);

        Assert.False(map.Contains("target"));
        Assert.False(map.Contains("absent"));
        Assert.Equal(1, map.Count);
        Assert.False(Assert.IsType<bool>(map["neighbor"]));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-HEADERS", "primitive-map-readers-preserve-zero-false-and-text")]
    public void Readers_AgreeOnNonNullNativeValuesAcrossAllEnumerationForms()
    {
        var map = new PrimitiveMap();
        map["zero"] = 0;
        map["false"] = false;
        map["text"] = "Grüße 東京";
        map["null"] = null;
        var headers = new PrimitiveMapHeaders(map);

        Assert.True(headers.TryGetHeader("zero", out object? zero));
        Assert.Equal(0, Assert.IsType<int>(zero));
        Assert.True(headers.TryGetHeader("false", out object? boolean));
        Assert.False(Assert.IsType<bool>(boolean));
        Assert.True(headers.TryGetHeader("text", out object? text));
        Assert.Equal("Grüße 東京", text);
        Assert.False(headers.TryGetHeader("null", out object? nullValue));
        Assert.Null(nullValue);
        Assert.False(headers.TryGetHeader("missing", out object? missing));
        Assert.Null(missing);

        AssertValues(headers.GetAll().ToDictionary(pair => pair.Key, pair => pair.Value));
        AssertValues(headers.ToDictionary(pair => pair.Key, pair => pair.Value));
        AssertValues(((IEnumerable)headers).Cast<HeaderValue>().ToDictionary(pair => pair.Key, pair => pair.Value));

        static void AssertValues(Dictionary<string, object> values)
        {
            Assert.Equal(new[] { "false", "text", "zero" }, values.Keys.Order());
            Assert.Equal(0, Assert.IsType<int>(values["zero"]));
            Assert.False(Assert.IsType<bool>(values["false"]));
            Assert.Equal("Grüße 東京", values["text"]);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-HEADERS", "primitive-map-invalid-writes-preserve-state")]
    public void InvalidKeys_RejectBothWriteOverloadsWithoutMutatingTheMap()
    {
        var map = new PrimitiveMap();
        map["retained"] = "value";
        var headers = new PrimitiveMapHeaders(map);

        Assert.Equal("key", Assert.Throws<ArgumentNullException>(() => headers.Set(null!, "text")).ParamName);
        Assert.Equal("key", Assert.Throws<ArgumentNullException>(() => headers.Set(null!, 5, true)).ParamName);
        Assert.Equal(1, map.Count);
        Assert.Equal("value", map["retained"]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-HEADERS", "primitive-map-typed-retrieval-fails-explicitly")]
    public void TypedRetrieval_RejectsReferenceAndValueTypesDespiteExistingValues()
    {
        var map = new PrimitiveMap();
        map["text"] = "value";
        map["number"] = 17;
        var headers = new PrimitiveMapHeaders(map);

        Assert.Throws<NotSupportedException>(() => headers.Get<string>("text", "fallback"));
        Assert.Throws<NotSupportedException>(() => headers.Get<int>("number", 3));
        Assert.Equal("value", map["text"]);
        Assert.Equal(17, map["number"]);
    }
}
