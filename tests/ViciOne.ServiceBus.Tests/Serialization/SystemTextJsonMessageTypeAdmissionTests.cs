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

    public sealed class ExpectedMessage
    {
        public int Value { get; init; }
    }

    public sealed class ShapeCompatibleButUnadvertisedMessage
    {
        public int Value { get; init; }
    }
}
