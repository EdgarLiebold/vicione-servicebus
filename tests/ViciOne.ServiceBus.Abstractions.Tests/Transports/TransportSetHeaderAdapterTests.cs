using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Transports;

public sealed class TransportSetHeaderAdapterTests
{
    [Theory]
    [InlineData((TransportHeaderOptions)0, false, false, false, false)]
    [InlineData(TransportHeaderOptions.IncludeHost, true, false, false, false)]
    [InlineData(TransportHeaderOptions.IncludeFaultMessage, false, true, false, false)]
    [InlineData(TransportHeaderOptions.IncludeFaultDetail, false, true, true, false)]
    [InlineData(TransportHeaderOptions.Default, false, true, true, false)]
    [InlineData(TransportHeaderOptions.IncludeHost | TransportHeaderOptions.Default, true, true, true, false)]
    [InlineData((TransportHeaderOptions)0, false, false, false, true)]
    [InlineData(TransportHeaderOptions.IncludeHost, true, false, false, true)]
    [InlineData(TransportHeaderOptions.IncludeFaultMessage, false, true, false, true)]
    [InlineData(TransportHeaderOptions.IncludeFaultDetail, false, true, true, true)]
    [InlineData(TransportHeaderOptions.Default, false, true, true, true)]
    [InlineData(TransportHeaderOptions.IncludeHost | TransportHeaderOptions.Default, true, true, true, true)]
    [RequirementCoverage("REQ-VSB-TRANSPORT-HEADER-FILTERING", "generic-adapter-option-matrix")]
    public void Options_FilterOnlyRequestedHostAndFaultHeadersBeforeConversion(
        TransportHeaderOptions options, bool includeHost, bool includeFaultMessage, bool includeFaultDetail, bool untyped)
    {
        var converter = new RecordingConverter();
        var adapter = new TransportSetHeaderAdapter<string>(converter, options);
        var headers = new Dictionary<string, string>();

        Set("ordinary", "value");
        Set(MessageHeaders.Host.MachineName, "machine");
        Set(MessageHeaders.Host.ProcessName, "process");
        Set(MessageHeaders.FaultInputAddress, "input");
        Set(MessageHeaders.FaultMessage, "failure");
        Set(MessageHeaders.FaultStackTrace, "trace");
        Set(MessageHeaders.FaultExceptionType, "type");

        Assert.Equal("converted:value", headers["ordinary"]);
        Assert.Equal("converted:input", headers[MessageHeaders.FaultInputAddress]);
        Assert.Equal(includeHost, headers.ContainsKey(MessageHeaders.Host.MachineName));
        Assert.Equal(includeHost, headers.ContainsKey(MessageHeaders.Host.ProcessName));
        Assert.Equal(includeFaultMessage, headers.ContainsKey(MessageHeaders.FaultMessage));
        Assert.Equal(includeFaultDetail, headers.ContainsKey(MessageHeaders.FaultStackTrace));
        Assert.Equal(includeFaultDetail, headers.ContainsKey(MessageHeaders.FaultExceptionType));
        Assert.Equal(2 + (includeHost ? 2 : 0) + (includeFaultMessage ? 1 : 0) + (includeFaultDetail ? 2 : 0), headers.Count);
        Assert.Equal(headers.Count, converter.Calls);
        if (includeHost)
        {
            Assert.Equal("converted:machine", headers[MessageHeaders.Host.MachineName]);
            Assert.Equal("converted:process", headers[MessageHeaders.Host.ProcessName]);
        }
        if (includeFaultMessage)
            Assert.Equal("converted:failure", headers[MessageHeaders.FaultMessage]);
        if (includeFaultDetail)
        {
            Assert.Equal("converted:trace", headers[MessageHeaders.FaultStackTrace]);
            Assert.Equal("converted:type", headers[MessageHeaders.FaultExceptionType]);
        }

        void Set(string key, string value)
        {
            if (untyped)
                adapter.Set(headers, new HeaderValue(key, value));
            else
                adapter.Set(headers, new HeaderValue<string>(key, value));
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TRANSPORT-HEADER-FILTERING", "generic-adapter-untyped-rejection")]
    public void UntypedConversionFailure_DoesNotReplaceTheExistingTransportValue()
    {
        var converter = new RecordingConverter();
        var adapter = new TransportSetHeaderAdapter<string>(converter);
        var headers = new Dictionary<string, string> { ["count"] = "previous" };

        adapter.Set(headers, new HeaderValue("count", 42));
        Assert.Equal("converted:42", headers["count"]);

        converter.Reject = true;
        adapter.Set(headers, new HeaderValue("count", 43));

        Assert.Equal("converted:42", headers["count"]);
        Assert.Single(headers);
        Assert.Equal(2, converter.Calls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TRANSPORT-HEADER-FILTERING", "generic-adapter-blank-removal")]
    public void BlankTypedValue_RemovesOnlyTheNamedExistingHeaderWithoutConversion()
    {
        var converter = new RecordingConverter();
        var adapter = new TransportSetHeaderAdapter<string>(converter);
        var headers = new Dictionary<string, string> { ["label"] = "old", ["other"] = "keep" };

        adapter.Set(headers, new HeaderValue<string>("label", " \t"));

        Assert.False(headers.ContainsKey("label"));
        Assert.Equal("keep", headers["other"]);
        Assert.Single(headers);
        Assert.Equal(0, converter.Calls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TRANSPORT-HEADER-FILTERING", "generic-adapter-required-converter")]
    public void Constructor_RejectsMissingConverterBeforeAnyHeaderIsSent()
    {
        var error = Assert.Throws<ArgumentNullException>(() => new TransportSetHeaderAdapter<string>(null!));

        Assert.Equal("converter", error.ParamName);
    }

    private sealed class RecordingConverter : IHeaderValueConverter<string>
    {
        public int Calls { get; private set; }
        public bool Reject { get; set; }

        public bool TryConvert(HeaderValue headerValue, out HeaderValue<string> result)
        {
            Calls++;
            if (Reject)
            {
                result = default;
                return false;
            }

            result = new HeaderValue<string>(headerValue.Key, "converted:" + headerValue.Value);
            return true;
        }

        public bool TryConvert<T>(HeaderValue<T> headerValue, out HeaderValue<string> result)
        {
            Calls++;
            if (Reject)
            {
                result = default;
                return false;
            }

            result = new HeaderValue<string>(headerValue.Key, "converted:" + headerValue.Value);
            return true;
        }
    }
}
