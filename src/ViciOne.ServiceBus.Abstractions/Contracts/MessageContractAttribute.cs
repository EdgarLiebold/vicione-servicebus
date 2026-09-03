using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Declares a stable application-level identity for a message contract that may cross a durable or wire boundary.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class MessageContractAttribute : Attribute
{
    public MessageContractAttribute(string name, int majorVersion = 1)
    {
        var identity = new MessageContractIdentity(name, majorVersion);
        Name = identity.Name;
        MajorVersion = identity.MajorVersion;
    }

    public string Name { get; }

    public int MajorVersion { get; }
}
