using System;

namespace ViciOne.ServiceBus;

/// <summary>Declares a stable application-level identity for a message contract that may cross a durable or wire boundary.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class MessageContractAttribute : Attribute
{
    /// <summary>Initializes the attribute with a stable contract name and major wire version.</summary>
    /// <param name="name">The stable, deployment-independent contract name.</param>
    /// <param name="majorVersion">The positive major version of the wire contract.</param>
    public MessageContractAttribute(string name, int majorVersion = 1)
    {
        var identity = new MessageContractIdentity(name, majorVersion);
        Name = identity.Name;
        MajorVersion = identity.MajorVersion;
    }

    /// <summary>Gets the stable, deployment-independent contract name.</summary>
    public string Name { get; }

    /// <summary>Gets the major version of the wire contract.</summary>
    public int MajorVersion { get; }
}
