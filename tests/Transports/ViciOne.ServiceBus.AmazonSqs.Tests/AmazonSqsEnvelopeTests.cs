using System.Text;
using System.Text.Json;
using Amazon.SQS.Model;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public sealed class AmazonSqsEnvelopeTests
{
    private const string MessageId = "00ab0000-6ab3-f8b4-f78c-08db7c8365ff";
    private const string Destination = "amazonsqs://us-east-1/orders";
    private const string Envelope = """
        {
          "messageId": "00ab0000-6ab3-f8b4-f78c-08db7c8365ff",
          "destinationAddress": "amazonsqs://us-east-1/orders",
          "messageTypes": ["urn:message:Orders:SubmitOrder"],
          "message": { "orderNumber": "A-1042", "quantity": 7 },
          "sentTime": "2026-08-27T05:06:07Z",
          "headers": { "tenant": "north" }
        }
        """;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-ENVELOPE", "cross-and-intra-region-exact-payload")]
    public void CrossAndIntraRegionBodies_DecodeToExactPayload(bool wrappedBySns)
    {
        string rawBody = wrappedBySns
            ? JsonSerializer.Serialize(new
            {
                Type = "Notification",
                MessageId = "10387869-22c1-4672-8a29-ca03a638476c",
                TopicArn = "arn:aws:sns:eu-west-1:123456789012:orders",
                Message = Envelope,
                Timestamp = "2026-09-12T10:11:12.123Z",
                SignatureVersion = "1",
                Signature = "AQID",
                SigningCertURL = "https://sns.eu-west-1.amazonaws.com/cert.pem",
            })
            : Envelope;
        var body = new SqsMessageBody(new Message { Body = rawBody }, wrappedBySns);

        JsonElement decoded = body.GetJsonElement(JsonSerializerOptions.Default)
            ?? throw new InvalidDataException("The SQS body did not decode to a JSON envelope.");

        Assert.Equal(rawBody, body.GetRequiredTransportText());
        Assert.Equal(Encoding.UTF8.GetBytes(rawBody), body.ToArray());
        Assert.True(body.TryGetPayloadText(out var payloadText));
        Assert.Equal(Envelope, payloadText);
        Assert.Equal(MessageId, decoded.GetProperty("messageId").GetString());
        Assert.Equal(Destination, decoded.GetProperty("destinationAddress").GetString());
        Assert.Equal("urn:message:Orders:SubmitOrder", decoded.GetProperty("messageTypes")[0].GetString());
        JsonElement payload = decoded.GetProperty("message");
        Assert.Equal("A-1042", payload.GetProperty("orderNumber").GetString());
        Assert.Equal(7, payload.GetProperty("quantity").GetInt32());
        Assert.Equal("north", decoded.GetProperty("headers").GetProperty("tenant").GetString());
        Assert.Equal(wrappedBySns ? "arn:aws:sns:eu-west-1:123456789012:orders" : null, body.TopicArn);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-ENVELOPE", "application-json-with-notification-fields-remains-direct")]
    public void DirectJsonWithNotificationLikeFields_IsNeverSilentlyUnwrapped()
    {
        const string applicationJson = """
            {
              "Type": "Notification",
              "TopicArn": "orders",
              "Message": { "value": 1 }
            }
            """;
        var body = new SqsMessageBody(new Message { Body = applicationJson });

        Assert.Equal(applicationJson, body.GetRequiredTransportText());
        Assert.True(body.TryGetPayloadText(out var payloadText));
        Assert.Equal(applicationJson, payloadText);
        Assert.Null(body.TopicArn);
        JsonElement root = body.GetJsonElement(JsonSerializerOptions.Default)
            ?? throw new InvalidDataException("The direct JSON body was not preserved.");
        Assert.Equal("Notification", root.GetProperty("Type").GetString());
        Assert.Equal(1, root.GetProperty("Message").GetProperty("value").GetInt32());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-ENVELOPE", "complete-sns-shaped-application-json-remains-direct-by-default")]
    public void CompleteSnsShapedApplicationJson_RemainsDirectUnlessUnwrappingIsExplicitlyEnabled()
    {
        string applicationJson = JsonSerializer.Serialize(new
        {
            Type = "Notification",
            MessageId = "10387869-22c1-4672-8a29-ca03a638476c",
            TopicArn = "arn:aws:sns:eu-central-1:123456789012:orders",
            Message = "{\"value\":1}",
            Timestamp = "2026-09-12T10:11:12.123Z",
            SignatureVersion = "1",
            Signature = "AQID",
            SigningCertURL = "https://sns.eu-central-1.amazonaws.com/cert.pem",
        });
        var body = new SqsMessageBody(new Message { Body = applicationJson });

        Assert.True(body.TryGetPayloadText(out var payloadText));
        Assert.Equal(applicationJson, payloadText);
        Assert.Null(body.TopicArn);
    }

    [Theory]
    [InlineData("Type", "ApplicationNotification")]
    [InlineData("MessageId", "not-a-guid")]
    [InlineData("TopicArn", "orders")]
    [InlineData("Timestamp", "not-a-timestamp")]
    [InlineData("SignatureVersion", "3")]
    [InlineData("Signature", " ")]
    [InlineData("SigningCertURL", "cert.pem")]
    [RequirementCoverage("REQ-VSB-AWS-SQS-ENVELOPE", "required-envelope-rejects-every-invalid-provider-discriminator")]
    public void RequiredNotificationEnvelope_RejectsEveryInvalidProviderDiscriminator(
        string field,
        string invalidValue)
    {
        var candidate = new Dictionary<string, object>
        {
            ["Type"] = "Notification",
            ["MessageId"] = "10387869-22c1-4672-8a29-ca03a638476c",
            ["TopicArn"] = "arn:aws:sns:eu-central-1:123456789012:orders",
            ["Message"] = "{\"value\":1}",
            ["Timestamp"] = "2026-09-12T10:11:12.123Z",
            ["SignatureVersion"] = "1",
            ["Signature"] = "AQID",
            ["SigningCertURL"] = "https://sns.eu-central-1.amazonaws.com/cert.pem",
        };
        candidate[field] = invalidValue;
        string applicationJson = JsonSerializer.Serialize(candidate);
        var body = new SqsMessageBody(new Message { Body = applicationJson }, true);

        Assert.Throws<InvalidDataException>(() => body.TryGetPayloadText(out _));
    }

    [Theory]
    [InlineData("Type")]
    [InlineData("MessageId")]
    [InlineData("TopicArn")]
    [InlineData("Message")]
    [InlineData("Timestamp")]
    [InlineData("SignatureVersion")]
    [InlineData("Signature")]
    [InlineData("SigningCertURL")]
    [RequirementCoverage("REQ-VSB-AWS-SQS-ENVELOPE", "required-envelope-rejects-every-missing-provider-field")]
    public void RequiredNotificationEnvelope_RejectsEveryMissingProviderField(string field)
    {
        var candidate = new Dictionary<string, object>
        {
            ["Type"] = "Notification",
            ["MessageId"] = "10387869-22c1-4672-8a29-ca03a638476c",
            ["TopicArn"] = "arn:aws:sns:eu-central-1:123456789012:orders",
            ["Message"] = "{\"value\":1}",
            ["Timestamp"] = "2026-09-12T10:11:12.123Z",
            ["SignatureVersion"] = "1",
            ["Signature"] = "AQID",
            ["SigningCertURL"] = "https://sns.eu-central-1.amazonaws.com/cert.pem",
        };
        Assert.True(candidate.Remove(field));
        var body = new SqsMessageBody(new Message { Body = JsonSerializer.Serialize(candidate) }, true);

        Assert.Throws<InvalidDataException>(() => body.TryGetPayloadText(out _));
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("[]")]
    [RequirementCoverage("REQ-VSB-AWS-SQS-ENVELOPE", "required-envelope-rejects-malformed-or-non-object-json")]
    public void RequiredNotificationEnvelope_RejectsMalformedOrNonObjectJson(string bodyText)
    {
        var body = new SqsMessageBody(new Message { Body = bodyText }, true);

        Assert.Throws<InvalidDataException>(() => body.TryGetPayloadText(out _));
    }
}
