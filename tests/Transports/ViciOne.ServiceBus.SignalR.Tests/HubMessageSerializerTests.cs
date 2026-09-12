using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using ViciOne.ServiceBus.SignalR.Runtime;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SignalR.Tests;

public sealed class HubMessageSerializerTests
{
    private static readonly IHubProtocol JsonProtocol = new JsonHubProtocol();

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-SERIALIZATION", "invalid-backplane-payloads-rejected")]
    public void ToSerializedHubMessage_InvalidPayloadCollections_AreRejected()
    {
        Assert.Equal(
            "protocolPayloads",
            Assert.Throws<ArgumentNullException>(() =>
                HubMessageSerializer.ToSerializedHubMessage(null!)).ParamName);
        Assert.Throws<InvalidDataException>(() =>
            new Dictionary<string, byte[]>().ToSerializedHubMessage());
        Assert.Throws<InvalidDataException>(() =>
            new Dictionary<string, byte[]> { [" "] = [1] }.ToSerializedHubMessage());
        Assert.Throws<InvalidDataException>(() =>
            new Dictionary<string, byte[]> { ["json"] = [] }.ToSerializedHubMessage());
        Assert.Throws<InvalidDataException>(() =>
            new Dictionary<string, byte[]> { ["json"] = null! }.ToSerializedHubMessage());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-SERIALIZATION", "invalid-invocation-inputs-rejected")]
    public void SerializeInvocation_InvalidInputs_AreRejected()
    {
        Assert.Equal(
            "protocols",
            Assert.Throws<ArgumentNullException>(() =>
                HubMessageSerializer.SerializeInvocation(null!, "Method", [])).ParamName);
        Assert.Equal(
            "methodName",
            Assert.Throws<ArgumentException>(() =>
                Array.Empty<IHubProtocol>().SerializeInvocation(" ", [])).ParamName);
        Assert.Equal(
            "args",
            Assert.Throws<ArgumentNullException>(() =>
                Array.Empty<IHubProtocol>().SerializeInvocation("Method", null!)).ParamName);
        Assert.Throws<InvalidOperationException>(() =>
            Array.Empty<IHubProtocol>().SerializeInvocation("Method", []));
        Assert.Throws<InvalidOperationException>(() =>
            new IHubProtocol[] { null! }.SerializeInvocation("Method", []));
        Assert.Throws<InvalidOperationException>(() =>
            new[] { JsonProtocol, JsonProtocol }.SerializeInvocation("Method", []));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-SERIALIZATION", "completion-round-trip")]
    public void CompletionFrame_RoundTripsExactlyOnce()
    {
        CompletionMessage expected = CompletionMessage.WithResult("invocation", 42);

        byte[] payload = HubMessageSerializer.SerializeCompletion(JsonProtocol, expected);
        CompletionMessage actual = HubMessageSerializer.DeserializeCompletion(
            JsonProtocol,
            payload,
            new IntegerResultInvocationBinder());

        Assert.Equal(expected.InvocationId, actual.InvocationId);
        Assert.True(actual.HasResult);
        Assert.Equal(42, Assert.IsType<int>(actual.Result));
        Assert.Null(actual.Error);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-SERIALIZATION", "non-completion-and-trailing-frames-rejected")]
    public void DeserializeCompletion_NonCompletionOrTrailingFrame_IsRejected()
    {
        byte[] invocation = new[] { JsonProtocol }.SerializeInvocation("Method", [42])["json"];
        byte[] completion = HubMessageSerializer.SerializeCompletion(
            JsonProtocol,
            CompletionMessage.WithResult("invocation", 42));
        byte[] duplicate = [.. completion, .. completion];

        Assert.Throws<InvalidDataException>(() =>
            HubMessageSerializer.DeserializeCompletion(
                JsonProtocol,
                invocation,
                new IntegerResultInvocationBinder()));
        Assert.Throws<InvalidDataException>(() =>
            HubMessageSerializer.DeserializeCompletion(
                JsonProtocol,
                duplicate,
                new IntegerResultInvocationBinder()));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-SERIALIZATION", "completion-null-inputs-rejected")]
    public void CompletionSerialization_NullInputs_AreRejected()
    {
        var binder = new IntegerResultInvocationBinder();
        CompletionMessage completion = CompletionMessage.WithResult("invocation", 42);

        Assert.Equal(
            "protocol",
            Assert.Throws<ArgumentNullException>(() =>
                HubMessageSerializer.SerializeCompletion(null!, completion)).ParamName);
        Assert.Equal(
            "completion",
            Assert.Throws<ArgumentNullException>(() =>
                HubMessageSerializer.SerializeCompletion(JsonProtocol, null!)).ParamName);
        Assert.Equal(
            "protocol",
            Assert.Throws<ArgumentNullException>(() =>
                HubMessageSerializer.DeserializeCompletion(null!, [], binder)).ParamName);
        Assert.Equal(
            "payload",
            Assert.Throws<ArgumentNullException>(() =>
                HubMessageSerializer.DeserializeCompletion(JsonProtocol, null!, binder)).ParamName);
        Assert.Equal(
            "invocationBinder",
            Assert.Throws<ArgumentNullException>(() =>
                HubMessageSerializer.DeserializeCompletion(JsonProtocol, [], null!)).ParamName);
    }

    private sealed class IntegerResultInvocationBinder : IInvocationBinder
    {
        public IReadOnlyList<Type> GetParameterTypes(string methodName) => [typeof(int)];

        public Type GetReturnType(string invocationId) => typeof(int);

        public Type GetStreamItemType(string streamId) => typeof(int);
    }
}
