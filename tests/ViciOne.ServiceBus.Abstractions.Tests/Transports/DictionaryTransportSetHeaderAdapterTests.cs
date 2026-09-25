using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Transports;

public sealed class DictionaryTransportSetHeaderAdapterTests
{
    [Fact]
    public void TypedValues_KeepTransportSafeScalarsAndTrimOnlyText()
    {
        var adapter = new DictionaryTransportSetHeaderAdapter(new SimpleHeaderValueConverter())
        {
            MaxHeaderLength = 3
        };
        var headers = new Dictionary<string, object>();

        adapter.Set(headers, new HeaderValue<string>("label", "abcdef"));
        adapter.Set(headers, new HeaderValue<int>("count", 12345));

        Assert.Equal(2, headers.Count);
        Assert.Equal("abc", headers["label"]);
        Assert.Equal(12345, headers["count"]);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" \t")]
    public void BlankTypedValue_RemovesOnlyItsExistingHeader(string blank)
    {
        var adapter = new DictionaryTransportSetHeaderAdapter(new SimpleHeaderValueConverter());
        var headers = new Dictionary<string, object>
        {
            ["label"] = "old",
            ["other"] = "keep"
        };

        adapter.Set(headers, new HeaderValue<string>("label", blank));

        Assert.False(headers.ContainsKey("label"));
        Assert.Equal("keep", headers["other"]);
        Assert.Single(headers);
    }

    [Fact]
    public void UnsupportedTypedValue_DoesNotReplaceExistingHeader()
    {
        var adapter = new DictionaryTransportSetHeaderAdapter(new SimpleHeaderValueConverter());
        var original = new object();
        var headers = new Dictionary<string, object> { ["complex"] = original };

        adapter.Set(headers, new HeaderValue<object>("complex", new object()));

        Assert.Same(original, headers["complex"]);
        Assert.Single(headers);
    }

    [Fact]
    public void DefaultOptions_ExcludeHostHeadersButPreserveFaultMessageAndDetail()
    {
        var adapter = new DictionaryTransportSetHeaderAdapter(new SimpleHeaderValueConverter());
        var headers = new Dictionary<string, object>();

        adapter.Set(headers, new HeaderValue<string>(MessageHeaders.Host.MachineName, "machine"));
        adapter.Set(headers, new HeaderValue<string>(MessageHeaders.FaultMessage, "failed"));
        adapter.Set(headers, new HeaderValue<string>(MessageHeaders.FaultStackTrace, "trace"));

        Assert.False(headers.ContainsKey(MessageHeaders.Host.MachineName));
        Assert.Equal("failed", headers[MessageHeaders.FaultMessage]);
        Assert.Equal("trace", headers[MessageHeaders.FaultStackTrace]);
        Assert.Equal(2, headers.Count);
    }

    [Fact]
    public void MessageOnlyOptions_ExcludeFaultDetailAndIncludeHostWhenRequested()
    {
        var adapter = new DictionaryTransportSetHeaderAdapter(
            new SimpleHeaderValueConverter(),
            TransportHeaderOptions.IncludeFaultMessage | TransportHeaderOptions.IncludeHost);
        var headers = new Dictionary<string, object>();

        adapter.Set(headers, new HeaderValue<string>(MessageHeaders.Host.MachineName, "machine"));
        adapter.Set(headers, new HeaderValue<string>(MessageHeaders.FaultMessage, "failed"));
        adapter.Set(headers, new HeaderValue<string>(MessageHeaders.FaultStackTrace, "trace"));

        Assert.Equal("machine", headers[MessageHeaders.Host.MachineName]);
        Assert.Equal("failed", headers[MessageHeaders.FaultMessage]);
        Assert.False(headers.ContainsKey(MessageHeaders.FaultStackTrace));
        Assert.Equal(2, headers.Count);
    }

    [Fact]
    public void UntypedValues_StoreSupportedScalarsAndDoNotReplaceRejectedValues()
    {
        var adapter = new DictionaryTransportSetHeaderAdapter(new SimpleHeaderValueConverter());
        var headers = new Dictionary<string, object> { ["complex"] = "existing" };

        adapter.Set(headers, new HeaderValue("count", 42));
        adapter.Set(headers, new HeaderValue("complex", new object()));

        Assert.Equal(42, headers["count"]);
        Assert.Equal("existing", headers["complex"]);
        Assert.Equal(2, headers.Count);
    }
}
