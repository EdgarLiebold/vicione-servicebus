using System;

namespace ViciOne.ServiceBus;

/// <summary>Declares a stable application-level identity for a message contract that may cross a durable or wire boundary.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class MessageContractAttribute : Attribute
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="name">The name.</param>
    /// <param name="majorVersion">The major version.</param>
    public MessageContractAttribute(string name, int majorVersion = 1)
    {
        var identity = new MessageContractIdentity(name, majorVersion);
        Name = identity.Name;
        MajorVersion = identity.MajorVersion;
    }

    /// <summary>Gets the name.</summary>
    public string Name { get; }

    /// <summary>Gets the major version.</summary>
    public int MajorVersion { get; }
}
