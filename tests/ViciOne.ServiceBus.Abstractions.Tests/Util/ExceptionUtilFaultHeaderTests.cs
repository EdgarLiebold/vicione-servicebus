using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Util;

public sealed class ExceptionUtilFaultHeaderTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-FAULT-MESSAGE-TYPE-METADATA", "unsafe-base-lookup-keeps-original-settlement-headers")]
    public void UnsafeBaseLookup_StillProducesTheOriginalFaultHeaders(bool nullBase)
    {
        var failure = new UnsafeBaseException(nullBase);
        var adapter = new DictionaryTransportSetHeaderAdapter(new SimpleHeaderValueConverter());

        (Dictionary<string, object> headers, string description) =
            ExceptionUtil.GetExceptionHeaderDetail(failure, adapter);

        Assert.Equal("fault", headers[MessageHeaders.Reason]);
        Assert.Equal("original settlement failure", description);
        Assert.Equal(description, headers[MessageHeaders.FaultMessage]);
        Assert.Contains(nameof(UnsafeBaseException),
            Assert.IsType<string>(headers[MessageHeaders.FaultExceptionType]), StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-MESSAGE-TYPE-METADATA", "safe-base-lookup-keeps-root-cause-headers")]
    public void SafeBaseLookup_StillUsesTheRootCauseForFaultHeaders()
    {
        var root = new TimeoutException("root timeout");
        var wrapper = new InvalidOperationException("outer failure", root);
        var adapter = new DictionaryTransportSetHeaderAdapter(new SimpleHeaderValueConverter());

        (Dictionary<string, object> headers, string description) =
            ExceptionUtil.GetExceptionHeaderDetail(wrapper, adapter);

        Assert.Equal("root timeout", description);
        Assert.Equal(description, headers[MessageHeaders.FaultMessage]);
        Assert.Contains(nameof(TimeoutException),
            Assert.IsType<string>(headers[MessageHeaders.FaultExceptionType]), StringComparison.Ordinal);
    }

    private sealed class UnsafeBaseException(bool nullBase) : Exception("original settlement failure")
    {
        public override Exception GetBaseException() => nullBase
            ? null!
            : throw new InvalidOperationException("base lookup failed");
    }
}
