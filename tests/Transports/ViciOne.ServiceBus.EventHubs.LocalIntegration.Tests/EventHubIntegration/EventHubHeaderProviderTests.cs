using Azure.Messaging.EventHubs;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EventHubs.LocalIntegration.Tests.EventHubIntegration;

public sealed class EventHubHeaderProviderTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-EVENTHUB-HEADERS", "missing-event-is-rejected-at-construction")]
    public void Constructor_RejectsMissingEventAtTheBoundary()
    {
        Assert.Equal("eventData", Assert.Throws<ArgumentNullException>(() => new EventHubHeaderProvider(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EVENTHUB-HEADERS", "enumeration-preserves-identifiers-and-non-null-properties")]
    public void GetAll_ProjectsPresentIdentifiersAndApplicationValuesWithoutNulls()
    {
        var eventData = new EventData(BinaryData.FromString("{}"))
        {
            MessageId = "broker-message",
            CorrelationId = "conversation-7",
        };
        eventData.Properties["X-Count"] = 42;
        eventData.Properties["X-Empty"] = "";
        eventData.Properties["X-Null"] = null!;

        KeyValuePair<string, object>[] headers = new EventHubHeaderProvider(eventData).GetAll().ToArray();

        Assert.Equal(4, headers.Length);
        Assert.Contains(headers, header => header.Key == MessageHeaders.MessageId && Equals(header.Value, "broker-message"));
        Assert.Contains(headers, header => header.Key == MessageHeaders.CorrelationId && Equals(header.Value, "conversation-7"));
        Assert.Contains(headers, header => header.Key == "X-Count" && Equals(header.Value, 42));
        Assert.Contains(headers, header => header.Key == "X-Empty" && Equals(header.Value, ""));
        Assert.DoesNotContain(headers, header => header.Key == "X-Null");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EVENTHUB-HEADERS", "blank-identifiers-are-absent-in-both-read-apis")]
    public void BlankIdentifiers_AreAbsentFromEnumerationAndLookup()
    {
        var eventData = new EventData(BinaryData.FromString("{}"))
        {
            MessageId = " \t ",
            CorrelationId = " ",
        };
        eventData.Properties["X-Count"] = 42;
        var provider = new EventHubHeaderProvider(eventData);

        KeyValuePair<string, object> header = Assert.Single(provider.GetAll());
        Assert.Equal("X-Count", header.Key);
        Assert.Equal(42, header.Value);
        Assert.False(provider.TryGetHeader("MESSAGEID", out object? messageId));
        Assert.Null(messageId);
        Assert.False(provider.TryGetHeader("correlationid", out object? correlationId));
        Assert.Null(correlationId);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EVENTHUB-HEADERS", "mapped-lookup-ignores-case-but-application-lookup-is-exact")]
    public void TryGetHeader_UsesCaseInsensitiveIdentifiersAndExactApplicationKeys()
    {
        var eventData = new EventData(BinaryData.FromString("{}"))
        {
            MessageId = "broker-message",
            CorrelationId = "conversation-7",
        };
        eventData.Properties["X-Trace"] = "trace-17";
        eventData.Properties["X-Null"] = null!;
        var provider = new EventHubHeaderProvider(eventData);

        Assert.True(provider.TryGetHeader("MESSAGEID", out object? messageId));
        Assert.Equal("broker-message", messageId);
        Assert.True(provider.TryGetHeader("correlationid", out object? correlationId));
        Assert.Equal("conversation-7", correlationId);
        Assert.True(provider.TryGetHeader("X-Trace", out object? trace));
        Assert.Equal("trace-17", trace);
        Assert.False(provider.TryGetHeader("x-trace", out object? wrongCase));
        Assert.Null(wrongCase);
        Assert.False(provider.TryGetHeader("X-Null", out object? nullValue));
        Assert.Null(nullValue);
        Assert.False(provider.TryGetHeader("missing", out object? missing));
        Assert.Null(missing);
    }
}
