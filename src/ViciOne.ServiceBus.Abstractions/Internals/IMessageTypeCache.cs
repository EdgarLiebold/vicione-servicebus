using System;
using System.Collections.Generic;
using System.Reflection;

namespace ViciOne.ServiceBus.Internals;

/// <summary>Exposes lazily computed metadata owned by a closed message-contract type.</summary>
internal interface IMessageTypeCache
{
    /// <summary>Gets the compact contract address used in diagnostic dimensions.</summary>
    string DiagnosticAddress { get; }

    /// <summary>Gets whether the type satisfies the message-contract rules.</summary>
    bool IsValidMessageType { get; }

    /// <summary>Gets the validation failure, or <see langword="null" /> when the type is valid.</summary>
    string? InvalidMessageTypeReason { get; }

    /// <summary>Gets whether the contract or one of its generic arguments is a non-public class.</summary>
    bool IsTemporaryMessageType { get; }

    /// <summary>Gets the contract and its eligible fault, base, and interface contracts.</summary>
    IReadOnlyList<Type> MessageTypes { get; }

    /// <summary>Gets the canonical URNs of all supported contracts.</summary>
    IReadOnlyList<string> MessageTypeNames { get; }

    /// <summary>Gets the readable instance properties exposed by the contract.</summary>
    IReadOnlyList<PropertyInfo> Properties { get; }
}
