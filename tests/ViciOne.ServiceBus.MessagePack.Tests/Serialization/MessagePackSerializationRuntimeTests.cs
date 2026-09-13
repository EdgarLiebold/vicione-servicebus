using System.Buffers;
using MessagePack;
using MessagePack.Formatters;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Events.Faults;
using ViciOne.ServiceBus.MessagePack.Serialization;
using ViciOne.ServiceBus.MessagePack.Serialization.Formatters;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.MessagePack.Tests.Serialization;

public sealed class MessagePackSerializationRuntimeTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-RESOLVER", "owned-contracts-coexist-with-fallback")]
    public void Options_ResolveOwnedContractsWithoutHidingFallbackContracts()
    {
        IMessagePackFormatter<Fault>? faultFormatter =
            MessagePackSerializationRuntime.Options.Resolver.GetFormatter<Fault>();
        IMessagePackFormatter<IRoutingSlip>? routingSlipFormatter =
            MessagePackSerializationRuntime.Options.Resolver.GetFormatter<IRoutingSlip>();

        Assert.IsType<InterfaceConcreteMapFormatter<Fault, FaultEvent>>(faultFormatter);
        Assert.IsType<InterfaceMessagePackFormatter<IRoutingSlip>>(routingSlipFormatter);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-RESOLVER", "all-owned-contract-mappings")]
    public void ServiceBusResolver_OwnsEveryDeclaredContractMapping()
    {
        ServiceBusMessagePackFormatterResolver resolver = ServiceBusMessagePackFormatterResolver.Instance;

        Assert.IsType<InterfaceConcreteMapFormatter<Fault, FaultEvent>>(resolver.GetFormatter<Fault>());
        Assert.IsType<InterfaceConcreteMapFormatter<ReceiveFault, ReceiveFaultEvent>>(
            resolver.GetFormatter<ReceiveFault>());
        Assert.IsType<InterfaceConcreteMapFormatter<ExceptionInfo, FaultExceptionInfo>>(
            resolver.GetFormatter<ExceptionInfo>());
        Assert.IsType<InterfaceConcreteMapFormatter<HostInfo, BusHostInfo>>(resolver.GetFormatter<HostInfo>());
        Assert.IsType<InterfaceConcreteMapFormatter<ScheduleMessage, ScheduleMessageCommand>>(
            resolver.GetFormatter<ScheduleMessage>());
        Assert.IsType<InterfaceConcreteMapFormatter<ScheduleRecurringMessage, ScheduleRecurringMessageCommand>>(
            resolver.GetFormatter<ScheduleRecurringMessage>());
        Assert.IsType<InterfaceConcreteMapFormatter<CancelScheduledMessage, CancelScheduledMessageCommand>>(
            resolver.GetFormatter<CancelScheduledMessage>());
        Assert.IsType<InterfaceConcreteMapFormatter<CancelScheduledRecurringMessage, CancelScheduledRecurringMessageCommand>>(
            resolver.GetFormatter<CancelScheduledRecurringMessage>());
        Assert.IsType<InterfaceConcreteMapFormatter<PauseScheduledRecurringMessage, PauseScheduledRecurringMessageCommand>>(
            resolver.GetFormatter<PauseScheduledRecurringMessage>());
        Assert.IsType<InterfaceConcreteMapFormatter<ResumeScheduledRecurringMessage, ResumeScheduledRecurringMessageCommand>>(
            resolver.GetFormatter<ResumeScheduledRecurringMessage>());
        Assert.IsType<MessageDataFormatter<string>>(
            resolver.GetFormatter<ViciOne.ServiceBus.Advanced.Serialization.MessageData<string>?>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-RESOLVER", "concrete-contracts-defer-to-fallback")]
    public void ServiceBusResolver_DefersConcreteContractsToTheCompositeFallback()
    {
        Assert.Null(ServiceBusMessagePackFormatterResolver.Instance.GetFormatter<BufferValue>());
        Assert.Null(ServiceBusMessagePackFormatterResolver.Instance.GetFormatter<List<int>>());
        Assert.NotNull(MessagePackSerializationRuntime.Options.Resolver.GetFormatter<BufferValue>());
        Assert.NotNull(MessagePackSerializationRuntime.Options.Resolver.GetFormatter<List<int>>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-SECURITY", "untrusted-data-mode")]
    public void Options_UseUntrustedDataSecurity()
    {
        Assert.Equal(MessagePackSecurity.UntrustedData, MessagePackSerializationRuntime.Options.Security);
        Assert.True(MessagePackSerializationRuntime.Options.Security.HashCollisionResistant);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-SECURITY", "hardened-dictionary-read")]
    public void HardenedOptions_ReadTheOverlayDictionaryShape()
    {
        var bytes = MessagePackSerializationRuntime.Serialize(
            new Dictionary<string, object> { ["id"] = 27, ["customer"] = "Frank" });

        var result = MessagePackSerializationRuntime.Deserialize<Dictionary<string, object>>(bytes);

        Assert.Equal(27, result["id"]);
        Assert.Equal("Frank", result["customer"]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-SERIALIZER", "runtime-boundaries-and-nil")]
    public void RuntimeOperations_ValidateOwnedInputsAndPreserveNil()
    {
        var writer = new ArrayBufferWriter<byte>();

        Assert.Equal("type", Assert.Throws<ArgumentNullException>(() =>
            MessagePackSerializationRuntime.Serialize(null!, writer, null)).ParamName);
        Assert.Equal("writer", Assert.Throws<ArgumentNullException>(() =>
            MessagePackSerializationRuntime.Serialize(typeof(BufferValue), null!, null)).ParamName);
        Assert.Equal("writer", Assert.Throws<ArgumentNullException>(() =>
            MessagePackSerializationRuntime.Serialize<BufferValue>(null!, new BufferValue())).ParamName);
        Assert.Equal("buffer", Assert.Throws<ArgumentNullException>(() =>
            MessagePackSerializationRuntime.Deserialize<BufferValue>((byte[])null!)).ParamName);
        Assert.Equal("messageType", Assert.Throws<ArgumentNullException>(() =>
            MessagePackSerializationRuntime.Deserialize(null!, [])).ParamName);
        Assert.Equal("buffer", Assert.Throws<ArgumentNullException>(() =>
            MessagePackSerializationRuntime.Deserialize(typeof(BufferValue), null!)).ParamName);

        MessagePackSerializationRuntime.Serialize(typeof(BufferValue), writer, null);
        Assert.Null(MessagePackSerializationRuntime.Deserialize(typeof(BufferValue), writer.WrittenSpan.ToArray()));
    }

    public sealed class BufferValue
    {
        public int Id { get; set; }
    }
}
