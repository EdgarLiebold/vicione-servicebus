using System.Net.Mime;
using System.Reflection;
using System.Text;
using Amazon.SQS;
using Amazon.SQS.Model;
using ViciOne.ServiceBus.AmazonSqs.Tests.TestDoubles;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public sealed class AmazonSqsReceiveContextTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-RECEIVE", "wire-carrier-remains-encoded-until-common-admission")]
    public void Body_PreservesTheWireCarrierUntilTheCommonAdmissionBoundary()
    {
        byte[] expected = [0xc1, 0x00, 0xff, 0x2a];
        string carrier = Convert.ToBase64String(expected);
        var contentType = new ContentType("application/vnd.example+msgpack");
        var message = new Message
        {
            Body = carrier,
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                [MessageHeaders.ContentType] = new() { DataType = "String", StringValue = contentType.ToString() },
            },
        };

        using AmazonSqsReceiveContext context = CreateContext(message);

        TransportTextMessageBody body = Assert.IsAssignableFrom<TransportTextMessageBody>(context.Body);
        Assert.IsType<SqsMessageBody>(body);
        Assert.Equal(Encoding.UTF8.GetByteCount(carrier), body.Length);
        Assert.Equal(Encoding.UTF8.GetBytes(carrier), body.ToArray());
        Assert.Equal(carrier, body.GetRequiredTransportText());
        Assert.True(body.TryGetPayloadText(out var payloadText));
        Assert.Equal(carrier, payloadText);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-RECEIVE", "sns-wrapped-binary-carrier-selects-envelope-content-type-after-admission")]
    public void NotificationEnvelope_PreservesCarrierAndSelectsItsDeclaredSerializer()
    {
        byte[] expected = [0xc1, 0x00, 0xff, 0x2a];
        string carrier = Convert.ToBase64String(expected);
        var expectedContentType = new ContentType("application/vnd.example+msgpack");
        string notification = System.Text.Json.JsonSerializer.Serialize(new
        {
            Type = "Notification",
            MessageId = "10387869-22c1-4672-8a29-ca03a638476c",
            TopicArn = "arn:aws:sns:eu-central-1:123456789012:orders",
            Message = carrier,
            Timestamp = "2026-09-12T10:11:12.123Z",
            SignatureVersion = "1",
            Signature = "AQID",
            SigningCertURL = "https://sns.eu-central-1.amazonaws.com/cert.pem",
            MessageAttributes = new Dictionary<string, object>
            {
                [MessageHeaders.ContentType] = new { Type = "String", Value = expectedContentType.ToString() },
            },
        });
        var message = new Message { Body = notification };

        using AmazonSqsReceiveContext context = CreateContext(message, requiresSnsNotificationEnvelope: true);

        Assert.Equal(Encoding.UTF8.GetByteCount(notification), context.Body.Length);
        Assert.Equal(expectedContentType, context.ContentType);
        TransportTextMessageBody body = Assert.IsAssignableFrom<TransportTextMessageBody>(context.Body);
        Assert.Equal(notification, body.GetRequiredTransportText());
        Assert.True(body.TryGetPayloadText(out var payloadText));
        Assert.Equal(carrier, payloadText);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-RECEIVE", "nullable-transport-property-contract")]
    public void TransportProperties_ReturnAnnotationMatchesTheNullableInterfaceContract()
    {
        MethodInfo method = typeof(AmazonSqsReceiveContext).GetMethod(nameof(AmazonSqsReceiveContext.GetTransportProperties))!;

        NullabilityInfo nullability = new NullabilityInfoContext().Create(method.ReturnParameter);

        Assert.Equal(NullabilityState.Nullable, nullability.ReadState);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-RECEIVE", "missing-fifo-properties-return-null")]
    public void TransportProperties_WithoutUsableFifoAttributes_ReturnsNull()
    {
        var message = new Message
        {
            Body = "{}",
            Attributes = new Dictionary<string, string>
            {
                [MessageSystemAttributeName.MessageGroupId] = " ",
                [MessageSystemAttributeName.MessageDeduplicationId] = string.Empty,
            },
        };
        using AmazonSqsReceiveContext context = CreateContext(message);

        IDictionary<string, object>? actual = context.GetTransportProperties();

        Assert.Null(actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-RECEIVE", "fifo-properties-use-canonical-keys")]
    public void TransportProperties_WithFifoAttributes_ReturnsExactCanonicalValues()
    {
        var message = new Message
        {
            Body = "{}",
            Attributes = new Dictionary<string, string>
            {
                [MessageSystemAttributeName.MessageGroupId] = "tenant-47",
                [MessageSystemAttributeName.MessageDeduplicationId] = "order-991",
            },
        };
        using AmazonSqsReceiveContext context = CreateContext(message);

        IDictionary<string, object> actual = Assert.IsAssignableFrom<IDictionary<string, object>>(
            context.GetTransportProperties());

        Assert.Equal(2, actual.Count);
        Assert.Equal("tenant-47", actual[AmazonSqsTransportPropertyNames.GroupId]);
        Assert.Equal("order-991", actual[AmazonSqsTransportPropertyNames.DeduplicationId]);
    }

    private static AmazonSqsReceiveContext CreateContext(
        Message message,
        ISerialization? serialization = null,
        bool requiresSnsNotificationEnvelope = false)
    {
        var defaultContentType = new ContentType("application/json");
        IMessageDeserializer defaultDeserializer = InterfaceProxy<IMessageDeserializer>.Create((method, arguments) => method.Name switch
        {
            nameof(IMessageDeserializer.GetMessageBody) => new StringMessageBody(Assert.IsType<string>(arguments![0])),
            "get_ContentType" => defaultContentType,
            _ => Default(method.ReturnType),
        });
        serialization ??= InterfaceProxy<ISerialization>.Create((method, _) => method.Name switch
        {
            "get_DefaultContentType" => defaultContentType,
            nameof(ISerialization.GetMessageDeserializer) => defaultDeserializer,
            _ => Default(method.ReturnType),
        });
        SqsReceiveEndpointContext endpointContext = InterfaceProxy<SqsReceiveEndpointContext>.Create((method, _) => method.Name switch
        {
            "get_InputAddress" => new Uri("amazonsqs://eu-central-1/orders"),
            "get_Serialization" => serialization,
            _ => Default(method.ReturnType),
        });
        ClientContext clientContext = InterfaceProxy<ClientContext>.Create((method, _) => Default(method.ReturnType));
        ReceiveSettings settings = InterfaceProxy<ReceiveSettings>.Create((method, _) => method.Name switch
        {
            "get_RequiresSnsNotificationEnvelope" => requiresSnsNotificationEnvelope,
            _ => Default(method.ReturnType),
        });
        ConnectionContext connectionContext = InterfaceProxy<ConnectionContext>.Create((method, _) => Default(method.ReturnType));

        return new AmazonSqsReceiveContext(message, false, endpointContext, clientContext, settings, connectionContext);
    }

    private static object? Default(Type returnType)
    {
        return returnType.IsValueType ? Activator.CreateInstance(returnType) : null;
    }
}
