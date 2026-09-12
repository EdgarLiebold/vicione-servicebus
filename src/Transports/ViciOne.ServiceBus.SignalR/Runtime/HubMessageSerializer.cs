using System.Buffers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace ViciOne.ServiceBus.SignalR.Runtime;

/// <summary>Converts hub protocol frames to and from transport-neutral backplane payloads.</summary>
internal static class HubMessageSerializer
{
    /// <summary>Builds a SignalR serialized message from protocol frames received through the backplane.</summary>
    public static SerializedHubMessage ToSerializedHubMessage(
        this IReadOnlyDictionary<string, byte[]> protocolPayloads)
    {
        ArgumentNullException.ThrowIfNull(protocolPayloads);

        if (protocolPayloads.Count == 0)
            throw new InvalidDataException("A SignalR backplane message must contain at least one protocol payload.");

        var messages = new List<SerializedMessage>(protocolPayloads.Count);
        foreach ((string protocolName, byte[] payload) in protocolPayloads)
        {
            if (string.IsNullOrWhiteSpace(protocolName))
                throw new InvalidDataException("A SignalR protocol name must not be empty.");
            if (payload is null || payload.Length == 0)
                throw new InvalidDataException($"The SignalR payload for protocol '{protocolName}' must not be empty.");

            messages.Add(new SerializedMessage(protocolName, payload));
        }

        return new SerializedHubMessage(messages);
    }

    /// <summary>Serializes an invocation once for every protocol supported by the publishing server.</summary>
    public static IReadOnlyDictionary<string, byte[]> SerializeInvocation(
        this IEnumerable<IHubProtocol> protocols,
        string methodName,
        object?[] args,
        string? invocationId = null)
    {
        ArgumentNullException.ThrowIfNull(protocols);
        ArgumentException.ThrowIfNullOrWhiteSpace(methodName);
        ArgumentNullException.ThrowIfNull(args);

        InvocationMessage invocation = invocationId is null
            ? new InvocationMessage(methodName, args)
            : new InvocationMessage(invocationId, methodName, args);
        var serializedMessage = new SerializedHubMessage(invocation);
        var payloads = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

        foreach (var protocol in protocols)
        {
            if (protocol is null)
                throw new InvalidOperationException("The SignalR protocol resolver returned a null protocol.");

            ReadOnlyMemory<byte> serialized = serializedMessage.GetSerializedMessage(protocol);
            if (!payloads.TryAdd(protocol.Name, serialized.ToArray()))
                throw new InvalidOperationException($"SignalR protocol '{protocol.Name}' was registered more than once.");
        }

        if (payloads.Count == 0)
            throw new InvalidOperationException("At least one SignalR hub protocol must be registered.");

        return payloads;
    }

    /// <summary>Encodes a client completion with the protocol used by the owning connection.</summary>
    public static byte[] SerializeCompletion(IHubProtocol protocol, CompletionMessage completion)
    {
        ArgumentNullException.ThrowIfNull(protocol);
        ArgumentNullException.ThrowIfNull(completion);

        var output = new ArrayBufferWriter<byte>();
        protocol.WriteMessage(completion, output);
        return output.WrittenSpan.ToArray();
    }

    /// <summary>Parses exactly one completion frame using the pending invocation's expected result type.</summary>
    public static CompletionMessage DeserializeCompletion(
        IHubProtocol protocol,
        byte[] payload,
        IInvocationBinder invocationBinder)
    {
        ArgumentNullException.ThrowIfNull(protocol);
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(invocationBinder);

        var input = new ReadOnlySequence<byte>(payload);
        if (!protocol.TryParseMessage(ref input, invocationBinder, out HubMessage? message) ||
            message is not CompletionMessage completion ||
            !input.IsEmpty)
        {
            throw new InvalidDataException("The SignalR backplane payload does not contain exactly one client completion.");
        }

        return completion;
    }
}
