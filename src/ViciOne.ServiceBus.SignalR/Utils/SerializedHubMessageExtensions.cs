using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace ViciOne.ServiceBus.SignalR.Utils;

/// <summary>
/// Provides extension methods for serialized hub message.
/// </summary>
public static class SerializedHubMessageExtensions
{
    /// <summary>
    /// Performs the to serialized hub message operation.
    /// </summary>
    /// <param name="protocolMessages">The protocol messages value.</param>
    /// <returns>The result of the operation.</returns>
    public static SerializedHubMessage ToSerializedHubMessage(this IReadOnlyDictionary<string, byte[]> protocolMessages)
    {
        return new SerializedHubMessage(protocolMessages.Select(message => new SerializedMessage(message.Key, message.Value)).ToList());
    }

    /// <summary>
    /// Performs the to protocol dictionary operation.
    /// </summary>
    /// <param name="protocols">The protocols value.</param>
    /// <param name="methodName">The method name value.</param>
    /// <param name="args">The args value.</param>
    /// <returns>The result of the operation.</returns>
    public static IReadOnlyDictionary<string, byte[]> ToProtocolDictionary(this IEnumerable<IHubProtocol> protocols, string methodName, object?[] args)
    {
        var serializedMessageHub = new SerializedHubMessage(new InvocationMessage(methodName, args));

        var messages = new Dictionary<string, byte[]>();

        foreach (var protocol in protocols)
        {
            ReadOnlyMemory<byte> serialized = serializedMessageHub.GetSerializedMessage(protocol);

            messages.Add(protocol.Name, serialized.ToArray());
        }

        return messages;
    }
}
