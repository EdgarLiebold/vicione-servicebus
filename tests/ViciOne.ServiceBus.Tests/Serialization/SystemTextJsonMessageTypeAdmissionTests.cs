using System.Reflection;
using System.Text.Json;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class SystemTextJsonMessageTypeAdmissionTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-JSON-TYPE-ADMISSION", "raw-options-have-named-zero-and-exact-composites")]
    public void RawSerializerOptions_ExposeNamedZeroAndExactCompositeValues()
    {
        Assert.Equal(0, (int)RawSerializerOptions.None);
        Assert.Equal(
            RawSerializerOptions.AddTransportHeaders | RawSerializerOptions.CopyHeaders,
            RawSerializerOptions.Default);
        Assert.Equal(
            RawSerializerOptions.AnyMessageType | RawSerializerOptions.AddTransportHeaders | RawSerializerOptions.CopyHeaders,
            RawSerializerOptions.All);
        Assert.Equal(nameof(RawSerializerOptions.None), RawSerializerOptions.None.ToString());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JSON-TYPE-ADMISSION", "raw-empty-type-list-denies-by-default")]
    public void RawEnvelopeWithoutTypeHeaders_DeniesEveryMessageTypeByDefault()
    {
        var serializer = new SystemTextJsonRawMessageSerializer(
            ServiceBusMetadataJson.Options,
            RawSerializerOptions.Default);
        SerializerContext context = serializer.Deserialize(
            new StringMessageBody("{\"value\":42}"),
            EmptyHeaders.Instance);

        bool generic = context.TryGetMessage<ExpectedMessage>(out ExpectedMessage? genericMessage);
        bool runtime = context.TryGetMessage(typeof(ExpectedMessage), out object? runtimeMessage);

        Assert.False(context.IsSupportedMessageType<ExpectedMessage>());
        Assert.False(context.IsSupportedMessageType(typeof(ExpectedMessage)));
        Assert.False(generic);
        Assert.Null(genericMessage);
        Assert.False(runtime);
        Assert.Null(runtimeMessage);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JSON-TYPE-ADMISSION", "raw-any-type-requires-explicit-opt-in")]
    public void RawEnvelopeWithoutTypeHeaders_AcceptsATypeOnlyWithExplicitAnyMessageType()
    {
        var serializer = new SystemTextJsonRawMessageSerializer(
            ServiceBusMetadataJson.Options,
            RawSerializerOptions.AnyMessageType);
        SerializerContext context = serializer.Deserialize(
            new StringMessageBody("{\"value\":42}"),
            EmptyHeaders.Instance);

        Assert.True(context.TryGetMessage<ExpectedMessage>(out ExpectedMessage? genericMessage));
        Assert.True(context.TryGetMessage(typeof(ExpectedMessage), out object? runtimeMessage));
        Assert.Equal(42, genericMessage.Value);
        Assert.Equal(42, Assert.IsType<ExpectedMessage>(runtimeMessage).Value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JSON-TYPE-ADMISSION", "runtime-type-honors-envelope-contract-list")]
    public void RuntimeTypeLookup_DoesNotDeserializeAnUnadvertisedContract()
    {
        SystemTextJsonRoundTripResult<ExpectedMessage> roundTrip =
            SystemTextJsonRoundTrip.ExecuteWithContext(new ExpectedMessage { Value = 42 });

        bool accepted = roundTrip.Context.TryGetMessage(typeof(ShapeCompatibleButUnadvertisedMessage), out object? message);

        Assert.False(roundTrip.Context.IsSupportedMessageType(typeof(ShapeCompatibleButUnadvertisedMessage)));
        Assert.False(accepted);
        Assert.Null(message);
        Assert.True(roundTrip.Context.TryGetMessage(typeof(ExpectedMessage), out object? expected));
        Assert.Equal(42, Assert.IsType<ExpectedMessage>(expected).Value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JSON-TYPE-ADMISSION", "body-consume-runtime-type-admission")]
    public void BodyConsumeContext_ReportsDeclaredAndRejectedRuntimeContracts()
    {
        SystemTextJsonRoundTripResult<ExpectedMessage> roundTrip =
            SystemTextJsonRoundTrip.ExecuteWithContext(new ExpectedMessage { Value = 42 });
        ReceiveContext receiveContext = DispatchProxy.Create<ReceiveContext, UnexpectedInvocationProxy>();
        var context = new BodyConsumeContext(receiveContext, roundTrip.Context);

        Assert.True(context.HasMessageType(typeof(ExpectedMessage)));
        Assert.False(context.HasMessageType(typeof(ShapeCompatibleButUnadvertisedMessage)));
        Assert.Equal("messageType", Assert.Throws<ArgumentNullException>(() => context.HasMessageType(null!)).ParamName);
        Assert.True(context.TryGetMessage<ExpectedMessage>(out ConsumeContext<ExpectedMessage>? message));
        Assert.Equal(42, message.Message.Value);
        Assert.False(context.TryGetMessage<ShapeCompatibleButUnadvertisedMessage>(out _));
        Assert.False(context.HasMessageType(typeof(ShapeCompatibleButUnadvertisedMessage)));
    }

    [Theory]
    [InlineData(nameof(BodyConsumeContext.SourceAddress))]
    [InlineData(nameof(BodyConsumeContext.DestinationAddress))]
    [InlineData(nameof(BodyConsumeContext.ResponseAddress))]
    [InlineData(nameof(BodyConsumeContext.FaultAddress))]
    [RequirementCoverage("REQ-VSB-JSON-TYPE-ADMISSION", "body-consume-missing-envelope-address-has-nullable-public-contract")]
    public void BodyConsumeContext_MissingEnvelopeAddressHasANullablePublicContract(string addressProperty)
    {
        var serializer = new SystemTextJsonMessageSerializer(ServiceBusMetadataJson.Options);
        var sendContext = new ViciOne.ServiceBus.Transports.MessageSendContext<ExpectedMessage>(
            new ExpectedMessage { Value = 42 }) { Serializer = serializer };
        SerializerContext deserialized = serializer.Deserialize(serializer.GetMessageBody(sendContext), EmptyHeaders.Instance);
        ReceiveContext receiveContext = DispatchProxy.Create<ReceiveContext, UnexpectedInvocationProxy>();
        var context = new BodyConsumeContext(receiveContext, deserialized);

        Assert.True(context.TryGetMessage<ExpectedMessage>(out ConsumeContext<ExpectedMessage>? message));
        Assert.Equal(42, message.Message.Value);
        PropertyInfo? property = typeof(BodyConsumeContext).GetProperty(addressProperty);
        PropertyInfo? contractProperty = typeof(MessageContext).GetProperty(addressProperty);
        Assert.NotNull(property);
        Assert.NotNull(contractProperty);
        Assert.Equal(typeof(Uri), property.PropertyType);
        Assert.Null(property.GetValue(context));
        var nullability = new NullabilityInfoContext();
        Assert.Equal(NullabilityState.Nullable, nullability.Create(contractProperty).ReadState);

        Assert.Equal(NullabilityState.Nullable, nullability.Create(property).ReadState);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JSON-TYPE-ADMISSION", "body-consume-present-envelope-addresses-survive-real-json-roundtrip")]
    public void BodyConsumeContext_PresentEnvelopeAddressesSurviveTheRealJsonRoundTrip()
    {
        SystemTextJsonRoundTripResult<ExpectedMessage> roundTrip =
            SystemTextJsonRoundTrip.ExecuteWithContext(new ExpectedMessage { Value = 73 });
        ReceiveContext receiveContext = DispatchProxy.Create<ReceiveContext, UnexpectedInvocationProxy>();
        var context = new BodyConsumeContext(receiveContext, roundTrip.Context);

        Assert.True(context.TryGetMessage<ExpectedMessage>(out ConsumeContext<ExpectedMessage>? message));
        Assert.Equal(73, message.Message.Value);
        Assert.Equal(new Uri("loopback://localhost/source"), context.SourceAddress);
        Assert.Equal(new Uri("loopback://localhost/destination"), context.DestinationAddress);
        Assert.Equal(new Uri("loopback://localhost/response"), context.ResponseAddress);
        Assert.Equal(new Uri("loopback://localhost/fault"), context.FaultAddress);
    }

    public sealed class ExpectedMessage
    {
        public int Value { get; init; }
    }

    public sealed class ShapeCompatibleButUnadvertisedMessage
    {
        public int Value { get; init; }
    }

    private class UnexpectedInvocationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == "get_PublishEndpointProvider")
                return DispatchProxy.Create<IPublishEndpointProvider, UnexpectedInvocationProxy>();

            throw new InvalidOperationException($"The body-consume contract invoked {targetMethod?.Name} unexpectedly.");
        }
    }
}
