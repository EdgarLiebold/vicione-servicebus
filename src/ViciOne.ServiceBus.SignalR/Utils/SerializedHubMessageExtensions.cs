using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace ViciOne.ServiceBus.SignalR.Utils;

/// <summary>Provides extension methods for serialized hub message.</summary>
public static class SerializedHubMessageExtensions
{
    /// <summary>Converts this value to serialized hub message.</summary>
    /// <param name="protocolMessages">The protocol messages.</param>
    /// <returns>The converted serialized hub message.</returns>
    public static SerializedHubMessage ToSerializedHubMessage(this IReadOnlyDictionary<string, byte[]> protocolMessages)
    {
        return new SerializedHubMessage(protocolMessages.Select(message => new SerializedMessage(message.Key, message.Value)).ToList());
    }

    /// <summary>Converts this value to protocol dictionary.</summary>
    /// <param name="protocols">The protocols.</param>
    /// <param name="methodName">The method name.</param>
    /// <param name="args">The args.</param>
    /// <returns>The converted protocol dictionary.</returns>
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
