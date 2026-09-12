using System.Text;
using System.Text.Json;
using global::Amazon.SQS;
using global::Amazon.SQS.Model;
using ViciOne.ServiceBus.AmazonSqs;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public sealed class AmazonSqsHeaderProviderTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-RECEIVE", "header-provider-rejects-missing-constructor-dependencies")]
    public void Constructor_RejectsMissingMessageOrBodyWithExactOwnership()
    {
        var message = new Message { Body = "payload" };
        var body = new SqsMessageBody(message);

        Assert.Equal(
            "message",
            Assert.Throws<ArgumentNullException>(() => new AmazonSqsHeaderProvider(null!, body)).ParamName);
        Assert.Equal(
            "body",
            Assert.Throws<ArgumentNullException>(() => new AmazonSqsHeaderProvider(message, null!)).ParamName);
        Assert.Equal(
            "key",
            Assert.Throws<ArgumentNullException>(() => body.TryGetNotificationHeader(null!, out _)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-RECEIVE", "absent-native-message-id-is-not-reported")]
    public void MissingNativeMessageId_DoesNotViolateTheSuccessfulHeaderLookupContract()
    {
        var message = new Message { Body = "payload", MessageId = null };
        var provider = new AmazonSqsHeaderProvider(message, new SqsMessageBody(message));

        Assert.False(provider.TryGetHeader(MessageHeaders.MessageId, out object? value));
        Assert.Null(value);
        Assert.DoesNotContain(provider.GetAll(), header => header.Key == MessageHeaders.MessageId);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-RECEIVE", "sns-envelope-message-attributes-become-transport-headers")]
    public void NotificationMessageAttributes_AreExposedWithoutLosingTheirNamesOrValues()
    {
        string bodyText = JsonSerializer.Serialize(new
        {
            Type = "Notification",
            MessageId = "10387869-22c1-4672-8a29-ca03a638476c",
            TopicArn = "arn:aws:sns:eu-central-1:123456789012:orders",
            Message = "payload",
            Timestamp = "2026-09-12T10:11:12.123Z",
            SignatureVersion = "1",
            Signature = "AQID",
            SigningCertURL = "https://sns.eu-central-1.amazonaws.com/cert.pem",
            MessageAttributes = new Dictionary<string, object>
            {
                [MessageHeaders.ContentType] = new { Type = "String", Value = "application/vnd.example+msgpack" },
                ["tenant"] = new { Type = "String", Value = "north" },
            },
        });
        var message = new Message { Body = bodyText, MessageId = "native-message-id" };
        var provider = new AmazonSqsHeaderProvider(message, new SqsMessageBody(message, true));

        Assert.True(provider.TryGetHeader(MessageHeaders.ContentType, out object? contentType));
        Assert.Equal("application/vnd.example+msgpack", contentType);
        Assert.True(provider.TryGetHeader("tenant", out object? tenant));
        Assert.Equal("north", tenant);

        Dictionary<string, object> all = provider.GetAll().ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
        Assert.Equal("native-message-id", all[MessageHeaders.MessageId]);
        Assert.Equal("application/vnd.example+msgpack", all[MessageHeaders.ContentType]);
        Assert.Equal("north", all["tenant"]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-RECEIVE", "native-sqs-attributes-override-sns-envelope-attributes")]
    public void NativeMessageAttribute_TakesPrecedenceOverTheNotificationAttribute()
    {
        string bodyText = JsonSerializer.Serialize(new
        {
            Type = "Notification",
            MessageId = "10387869-22c1-4672-8a29-ca03a638476c",
            TopicArn = "arn:aws:sns:eu-central-1:123456789012:orders",
            Message = "payload",
            Timestamp = "2026-09-12T10:11:12.123Z",
            SignatureVersion = "1",
            Signature = "AQID",
            SigningCertURL = "https://sns.eu-central-1.amazonaws.com/cert.pem",
            MessageAttributes = new Dictionary<string, object>
            {
                ["tenant"] = new { Type = "String", Value = "notification" },
            },
        });
        var message = new Message
        {
            Body = bodyText,
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                ["tenant"] = new() { DataType = "String", StringValue = "queue" },
            },
        };
        var provider = new AmazonSqsHeaderProvider(message, new SqsMessageBody(message, true));

        Assert.True(provider.TryGetHeader("tenant", out object? tenant));
        Assert.Equal("queue", tenant);
        Assert.Single(provider.GetAll(), x => x.Key == "tenant" && Equals(x.Value, "queue"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-RECEIVE", "malformed-sns-envelope-attributes-are-rejected-after-native-admission")]
    public void MalformedNotificationAttribute_IsRejectedOnlyWhenEnvelopeMetadataIsRead()
    {
        const string bodyText = """
            {
              "Type": "Notification",
              "MessageId": "10387869-22c1-4672-8a29-ca03a638476c",
              "TopicArn": "arn:aws:sns:eu-central-1:123456789012:orders",
              "Message": "payload",
              "Timestamp": "2026-09-12T10:11:12.123Z",
              "SignatureVersion": "1",
              "Signature": "AQID",
              "SigningCertURL": "https://sns.eu-central-1.amazonaws.com/cert.pem",
              "MessageAttributes": { "tenant": { "Type": "String" } }
            }
            """;
        var message = new Message { Body = bodyText };
        var body = new SqsMessageBody(message, true);
        var provider = new AmazonSqsHeaderProvider(message, body);

        Assert.Equal(Encoding.UTF8.GetByteCount(bodyText), body.Length);
        Assert.Throws<InvalidDataException>(() => provider.TryGetHeader("tenant", out _));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-RECEIVE", "provider-sent-timestamp-parses-exact-unix-milliseconds")]
    public void ProviderSentTimestamp_ParsesExactUnixMillisecondsAsUtc()
    {
        var message = new Message
        {
            Body = "{}",
            Attributes = new Dictionary<string, string>
            {
                [MessageSystemAttributeName.SentTimestamp] = "2154060428123",
            },
        };
        var provider = new AmazonSqsHeaderProvider(message, new SqsMessageBody(message));

        Assert.True(provider.TryGetHeader(MessageHeaders.TransportSentTime, out object? value));
        DateTime actual = Assert.IsType<DateTime>(value);

        Assert.Equal(new DateTime(2038, 4, 5, 6, 7, 8, 123, DateTimeKind.Utc), actual);
        Assert.Equal(DateTimeKind.Utc, actual.Kind);
    }
}
