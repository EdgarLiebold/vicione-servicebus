using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.AzureServiceBus;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests;

public sealed class ServiceBusHeaderProviderTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-HEADERS", "received-message-is-required")]
    public void Constructor_RejectsAMissingReceivedMessage()
    {
        Assert.Equal(
            "message",
            Assert.Throws<ArgumentNullException>(() => new ServiceBusHeaderProvider(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-HEADERS", "azure-diagnostic-id-projects-to-canonical-activity-id")]
    public void AzureDiagnosticId_ProjectsToTheCanonicalActivityHeader()
    {
        const string activityId = "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01";
        ServiceBusReceivedMessage message = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("{}"),
            properties: new Dictionary<string, object>
            {
                ["Diagnostic-Id"] = activityId,
            });
        var provider = new ServiceBusHeaderProvider(message);

        bool found = provider.TryGetHeader(MessageHeaders.Prefix + "Activity-Id", out object? value);

        Assert.True(found);
        Assert.Equal(activityId, value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-HEADERS", "system-and-application-enumeration-preserves-values-but-excludes-spoofed-time")]
    public void GetAll_ProjectsPresentSystemAndApplicationHeadersWithoutSpoofedTime()
    {
        ServiceBusReceivedMessage message = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("{}"),
            messageId: "broker-message",
            correlationId: "conversation-7",
            contentType: "application/json",
            properties: new Dictionary<string, object>
            {
                ["X-Customer"] = 42,
                ["X-Empty"] = "",
                [MessageHeaders.TransportSentTime] = "forged-time",
                [MessageHeaders.TransportSentTime.ToLowerInvariant()] = "another-forged-time",
            });

        Dictionary<string, object> headers = new ServiceBusHeaderProvider(message).GetAll()
            .ToDictionary(header => header.Key, header => header.Value);

        Assert.Equal(5, headers.Count);
        Assert.Equal("broker-message", headers[MessageHeaders.MessageId]);
        Assert.Equal("conversation-7", headers[nameof(message.CorrelationId)]);
        Assert.Equal("application/json", headers[MessageHeaders.ContentType]);
        Assert.Equal(42, headers["X-Customer"]);
        Assert.Equal("", headers["X-Empty"]);
        Assert.DoesNotContain(headers.Keys,
            key => MessageHeaders.TransportSentTime.Equals(key, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-HEADERS", "blank-system-values-are-not-enumerated")]
    public void GetAll_OmitsBlankSystemValues()
    {
        ServiceBusReceivedMessage empty = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("{}"),
            messageId: " ",
            correlationId: " ",
            contentType: " ");
        Assert.Empty(new ServiceBusHeaderProvider(empty).GetAll());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-HEADERS", "application-lookup-is-exact-and-system-lookup-is-case-insensitive")]
    public void TryGetHeader_RequiresExactApplicationNameAndIgnoresSystemCase()
    {
        ServiceBusReceivedMessage message = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("{}"),
            messageId: "broker-message",
            properties: new Dictionary<string, object> { ["X-Trace"] = "trace-17" });
        var provider = new ServiceBusHeaderProvider(message);

        Assert.True(provider.TryGetHeader("X-Trace", out object? applicationValue));
        Assert.Equal("trace-17", applicationValue);
        Assert.False(provider.TryGetHeader("x-trace", out object? differentCase));
        Assert.Null(differentCase);
        Assert.True(provider.TryGetHeader("MESSAGEID", out object? messageId));
        Assert.Equal("broker-message", messageId);
        Assert.False(provider.TryGetHeader("missing", out object? missing));
        Assert.Null(missing);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-HEADERS", "raw-identity-headers-retain-canonical-guid-format")]
    public void RawIdentityHeaders_TakePrecedenceOverBrokerFormatting()
    {
        Guid messageId = Guid.Parse("c864d47d-6ba1-4f35-ae5c-8981699e6d03");
        Guid correlationId = Guid.Parse("532543d2-6c79-4eb5-b46f-a0f59a8f1982");
        ServiceBusReceivedMessage message = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("{}"),
            messageId: messageId.ToString("N"),
            correlationId: correlationId.ToString("N"),
            properties: new Dictionary<string, object>
            {
                [MessageHeaders.MessageId] = messageId.ToString("D"),
                [MessageHeaders.CorrelationId] = correlationId.ToString("D"),
            });
        var provider = new ServiceBusHeaderProvider(message);

        Assert.True(provider.TryGetHeader(MessageHeaders.MessageId, out object? rawMessageId));
        Assert.Equal(messageId.ToString("D"), rawMessageId);
        Assert.True(provider.TryGetHeader(MessageHeaders.CorrelationId, out object? rawCorrelationId));
        Assert.Equal(correlationId.ToString("D"), rawCorrelationId);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-HEADERS", "transport-sent-time-is-exact-utc-broker-time")]
    public void TransportSentTime_UsesTheBrokerEnqueueInstantInUtc()
    {
        DateTimeOffset enqueuedTime = new(2044, 5, 6, 7, 8, 9, TimeSpan.FromHours(2));
        ServiceBusReceivedMessage message = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("{}"),
            enqueuedTime: enqueuedTime,
            properties: new Dictionary<string, object>
            {
                [MessageHeaders.TransportSentTime] = "forged-time",
                [MessageHeaders.TransportSentTime.ToLowerInvariant()] = "another-forged-time",
            });

        var provider = new ServiceBusHeaderProvider(message);
        Assert.True(provider.TryGetHeader(MessageHeaders.TransportSentTime, out object? value));
        DateTime actual = Assert.IsType<DateTime>(value);
        Assert.Equal(enqueuedTime.UtcDateTime, actual);
        Assert.Equal(DateTimeKind.Utc, actual.Kind);
        Assert.True(provider.TryGetHeader(MessageHeaders.TransportSentTime.ToUpperInvariant(), out object? changedCase));
        Assert.Equal(actual, changedCase);
    }
}
