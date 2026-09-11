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
                TopicArn = "arn:aws:sns:eu-west-1:123456789012:orders",
                Message = Envelope
            })
            : Envelope;
        var body = new SqsMessageBody(new Message { Body = rawBody });

        JsonElement decoded = body.GetJsonElement(JsonSerializerOptions.Default)
            ?? throw new InvalidDataException("The SQS body did not decode to a JSON envelope.");

        Assert.Equal(MessageId, decoded.GetProperty("messageId").GetString());
        Assert.Equal(Destination, decoded.GetProperty("destinationAddress").GetString());
        Assert.Equal("urn:message:Orders:SubmitOrder", decoded.GetProperty("messageTypes")[0].GetString());
        JsonElement payload = decoded.GetProperty("message");
        Assert.Equal("A-1042", payload.GetProperty("orderNumber").GetString());
        Assert.Equal(7, payload.GetProperty("quantity").GetInt32());
        Assert.Equal("north", decoded.GetProperty("headers").GetProperty("tenant").GetString());
        Assert.Equal(wrappedBySns ? "arn:aws:sns:eu-west-1:123456789012:orders" : null, body.TopicArn);
    }
}
