using ViciOne.ServiceBus.JobService.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.JobService.Scheduling;

public sealed class TimeZoneResolverTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TIME-ZONE-RESOLUTION", "caller-owned-resolvers-do-not-cross-talk")]
    public void SameUnknownIdentifier_CanBeResolvedDifferentlyByIndependentOwners()
    {
        string unknownId = $"vsb-missing-{NewId.NextGuid():N}";
        TimeZoneInfo first = TimeZoneInfo.CreateCustomTimeZone(
            "vsb-owner-a",
            TimeSpan.FromHours(2),
            "Owner A",
            "Owner A");
        TimeZoneInfo second = TimeZoneInfo.CreateCustomTimeZone(
            "vsb-owner-b",
            TimeSpan.FromHours(-5),
            "Owner B",
            "Owner B");
        var firstCalls = 0;
        var secondCalls = 0;

        TimeZoneInfo firstResult = TimeZoneResolver.FindTimeZoneById(unknownId, id =>
        {
            Assert.Equal(unknownId, id);
            Interlocked.Increment(ref firstCalls);
            return first;
        });
        TimeZoneInfo secondResult = TimeZoneResolver.FindTimeZoneById(unknownId, id =>
        {
            Assert.Equal(unknownId, id);
            Interlocked.Increment(ref secondCalls);
            return second;
        });

        Assert.Same(first, firstResult);
        Assert.Same(second, secondResult);
        Assert.Equal(1, Volatile.Read(ref firstCalls));
        Assert.Equal(1, Volatile.Read(ref secondCalls));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TIME-ZONE-RESOLUTION", "system-zone-bypasses-custom-resolver")]
    public void SystemIdentifier_DoesNotInvokeTheOwnerResolver()
    {
        var calls = 0;

        TimeZoneInfo result = TimeZoneResolver.FindTimeZoneById(TimeZoneInfo.Utc.Id, _ =>
        {
            Interlocked.Increment(ref calls);
            return null;
        });

        Assert.Equal(TimeZoneInfo.Utc.Id, result.Id);
        Assert.Equal(0, Volatile.Read(ref calls));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TIME-ZONE-RESOLUTION", "unresolved-zone-preserves-domain-failure")]
    public void UnknownIdentifierWithoutMatchingResolver_ThrowsTheDocumentedFailure()
    {
        string unknownId = $"vsb-missing-{NewId.NextGuid():N}";

        TimeZoneNotFoundException exception = Assert.Throws<TimeZoneNotFoundException>(() =>
            TimeZoneResolver.FindTimeZoneById(unknownId, _ => null));

        Assert.Contains(unknownId, exception.Message, StringComparison.Ordinal);
        Assert.IsType<TimeZoneNotFoundException>(exception.InnerException);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [RequirementCoverage("REQ-VSB-TIME-ZONE-RESOLUTION", "missing-identifiers-are-rejected")]
    public void MissingIdentifier_IsRejected(string? id)
    {
        ArgumentException exception = Assert.ThrowsAny<ArgumentException>(() => TimeZoneResolver.FindTimeZoneById(id!));

        Assert.Equal("id", exception.ParamName);
    }
}
