using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Clients;

public sealed class RequestTimeoutTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-TIMEOUT", "default-none-and-or")]
    public void DefaultNoneAndOr_HaveExactValueSemantics()
    {
        RequestTimeout none = RequestTimeout.None;
        RequestTimeout fallback = RequestTimeout.After(s: 17);

        Assert.False(none.HasValue);
        Assert.Throws<InvalidOperationException>(() => none.Value);
        Assert.True(RequestTimeout.Default.HasValue);
        Assert.Equal(TimeSpan.FromSeconds(30), RequestTimeout.Default.Value);
        Assert.Equal(fallback, none.Or(fallback));
        Assert.Equal(RequestTimeout.Default, RequestTimeout.Default.Or(fallback));
        Assert.Equal(RequestTimeout.None, default(RequestTimeout));
        Assert.Equal(RequestTimeout.None.GetHashCode(), default(RequestTimeout).GetHashCode());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-TIMEOUT", "time-span-and-millisecond-conversions")]
    public void ImplicitConversions_PreserveTheExactRequestedDuration()
    {
        RequestTimeout fromTimeSpan = TimeSpan.FromMilliseconds(725);
        RequestTimeout fromMilliseconds = 125;

        Assert.Equal(TimeSpan.FromMilliseconds(725), fromTimeSpan.Value);
        Assert.Equal(TimeSpan.FromMilliseconds(125), fromMilliseconds.Value);
        Assert.NotEqual(fromTimeSpan, fromMilliseconds);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-TIMEOUT", "component-composition")]
    public void After_ComposesEveryDurationComponentExactly()
    {
        RequestTimeout timeout = RequestTimeout.After(d: 1, h: 2, m: 3, s: 4, ms: 5);

        Assert.Equal(new TimeSpan(1, 2, 3, 4, 5), timeout.Value);
        Assert.Equal(timeout, RequestTimeout.After(d: 1, h: 2, m: 3, s: 4, ms: 5));
        Assert.True(timeout == RequestTimeout.After(d: 1, h: 2, m: 3, s: 4, ms: 5));
        Assert.False(timeout != RequestTimeout.After(d: 1, h: 2, m: 3, s: 4, ms: 5));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [RequirementCoverage("REQ-VSB-REQUEST-TIMEOUT", "non-positive-conversion-rejected")]
    public void NonPositiveImplicitDurations_AreRejected(int milliseconds)
    {
        ArgumentOutOfRangeException integerException = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            RequestTimeout _ = milliseconds;
        });
        ArgumentOutOfRangeException timeSpanException = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            RequestTimeout _ = TimeSpan.FromMilliseconds(milliseconds);
        });

        Assert.Equal("milliseconds", integerException.ParamName);
        Assert.Equal("timeout", timeSpanException.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-TIMEOUT", "empty-composition-rejected")]
    public void EmptyAfterComposition_IsRejected()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => RequestTimeout.After());

        Assert.Equal("The timeout must be > 0", exception.Message);
    }
}
