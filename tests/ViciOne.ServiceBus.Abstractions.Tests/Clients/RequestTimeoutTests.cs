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
        var fallback = new RequestTimeout(TimeSpan.FromSeconds(17));

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
    [RequirementCoverage("REQ-VSB-REQUEST-TIMEOUT", "explicit-duration-construction")]
    public void Constructor_PreservesTheExactRequestedDuration()
    {
        var timeout = new RequestTimeout(TimeSpan.FromMilliseconds(725));

        Assert.Equal(TimeSpan.FromMilliseconds(725), timeout.Value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-TIMEOUT", "unambiguous-public-surface")]
    public void PublicSurface_RequiresAnExplicitTimeSpanUnit()
    {
        Type timeoutType = typeof(RequestTimeout);
        string[] conversionOrComponentFactories = timeoutType
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.DeclaredOnly)
            .Where(method => method.Name is "op_Implicit" or "After")
            .Select(method => method.Name)
            .ToArray();

        Assert.NotNull(timeoutType.GetConstructor([typeof(TimeSpan)]));
        Assert.Empty(conversionOrComponentFactories);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-TIMEOUT", "value-equality")]
    public void Equality_UsesTheExactDuration()
    {
        var first = new RequestTimeout(TimeSpan.FromMinutes(2));
        var same = new RequestTimeout(TimeSpan.FromMinutes(2));
        var different = new RequestTimeout(TimeSpan.FromMinutes(3));

        Assert.Equal(first, same);
        Assert.NotEqual(first, different);
        Assert.True(first == same);
        Assert.True(first != different);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [RequirementCoverage("REQ-VSB-REQUEST-TIMEOUT", "non-positive-duration-rejected")]
    public void Constructor_RejectsNonPositiveDurations(int milliseconds)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new RequestTimeout(TimeSpan.FromMilliseconds(milliseconds)));

        Assert.Equal("timeout", exception.ParamName);
        Assert.Equal(TimeSpan.FromMilliseconds(milliseconds), exception.ActualValue);
    }
}
