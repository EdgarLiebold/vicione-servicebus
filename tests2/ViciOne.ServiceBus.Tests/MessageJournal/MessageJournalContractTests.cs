using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.MessageJournal;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.MessageJournal;

public sealed class MessageJournalContractTests
{
    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(-1, 1, 1)]
    [InlineData(1, 0, 1)]
    [InlineData(1, -1, 1)]
    [InlineData(1, 1, 0)]
    [InlineData(1, 1, -1)]
    [RequirementCoverage("REQ-VSB-MESSAGE-JOURNAL-BOUNDS", "non-positive-store-limits-rejected")]
    public void StoreLimits_RejectEveryNonPositiveBoundary(int maximumBytes, int maximumEntries, int retentionTicks)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MessageJournalStoreLimits(
            maximumBytes,
            maximumEntries,
            TimeSpan.FromTicks(retentionTicks)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-JOURNAL-BOUNDS", "infinite-retention-rejected")]
    public void StoreLimits_RejectInfiniteRetention()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MessageJournalStoreLimits(
            maximumEntryBytes: 1024,
            maximumEntries: 100,
            Timeout.InfiniteTimeSpan));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [RequirementCoverage("REQ-VSB-MESSAGE-JOURNAL-FAILURE-CONTRACT", "non-positive-write-timeout-rejected")]
    public void Options_RejectNonPositiveWriteTimeout(long ticks)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MessageJournalOptions.ContinueMessageFlow(
            TimeSpan.FromTicks(ticks),
            TimeProvider.System));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-JOURNAL-FAILURE-CONTRACT", "finite-timeout-and-clock-preserved")]
    public void Options_PreserveTheExplicitFiniteTimeoutAndClock()
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero));
        TimeSpan writeTimeout = TimeSpan.FromSeconds(7);

        MessageJournalOptions options = MessageJournalOptions.ContinueMessageFlow(writeTimeout, timeProvider);

        Assert.Equal(writeTimeout, options.WriteTimeout);
        Assert.Same(timeProvider, options.TimeProvider);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-JOURNAL-FAILURE-CONTRACT", "unsupported-timer-delay-rejected-at-composition")]
    public void Options_RejectAWriteTimeoutTheRuntimeTimerCannotRepresent()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MessageJournalOptions.ContinueMessageFlow(
            TimeSpan.MaxValue,
            TimeProvider.System));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-JOURNAL-SNAPSHOT", "projection-detaches-caller-owned-inputs")]
    public void Projection_DetachesEveryCallerOwnedInput()
    {
        string[] messageTypes = ["urn:message:original"];
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal) { ["tenant"] = "north" };
        var headers = new Dictionary<string, string>(StringComparer.Ordinal) { ["trace"] = "kept" };
        byte[] body = [1, 2, 3];

        var projection = new MessageJournalProjection(
            MessageJournalDataClassification.Internal,
            "application/json",
            messageTypes,
            metadata,
            headers,
            body);

        messageTypes[0] = "urn:message:changed";
        metadata["tenant"] = "south";
        headers["trace"] = "changed";
        body[0] = 9;

        Assert.Equal(["urn:message:original"], projection.MessageTypes);
        Assert.Equal("north", projection.Metadata["tenant"]);
        Assert.Equal("kept", projection.Headers["trace"]);
        Assert.Equal(new byte[] { 1, 2, 3 }, projection.Body.ToArray());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-JOURNAL-CLASSIFICATION", "undefined-classification-rejected")]
    public void Projection_RejectsAnUndefinedDataClassification()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MessageJournalProjection(
            (MessageJournalDataClassification)0,
            contentType: null));
    }
}
