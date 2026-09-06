using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests;

public sealed class MessageTooLargeExceptionTests
{
    private static readonly Uri InputAddress = new("loopback://localhost/oversized");

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-TOO-LARGE", "complete-diagnostics")]
    public void Constructor_PreservesEveryDiagnosticValue()
    {
        var exception = new MessageTooLargeException(65, 64, InputAddress);

        Assert.Equal(65, exception.ActualBytes);
        Assert.Equal(64, exception.MaximumBytes);
        Assert.Equal(InputAddress, exception.InputAddress);
        Assert.Contains("65 bytes", exception.Message, StringComparison.Ordinal);
        Assert.Contains("64 bytes", exception.Message, StringComparison.Ordinal);
        Assert.Contains(InputAddress.AbsoluteUri, exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(-1, 64, "actualBytes")]
    [InlineData(65, 0, "maximumBytes")]
    [InlineData(64, 64, "actualBytes")]
    [InlineData(63, 64, "actualBytes")]
    [RequirementCoverage("REQ-VSB-MESSAGE-TOO-LARGE", "size-invariants")]
    public void Constructor_RejectsInvalidSizeRelationships(long actualBytes, long maximumBytes, string parameterName)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MessageTooLargeException(actualBytes, maximumBytes, InputAddress));

        Assert.Equal(parameterName, exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-TOO-LARGE", "required-input-address")]
    public void Constructor_RejectsAMissingInputAddress()
    {
        Assert.Equal(
            "inputAddress",
            Assert.Throws<ArgumentNullException>(() => new MessageTooLargeException(65, 64, null!)).ParamName);
    }
}
