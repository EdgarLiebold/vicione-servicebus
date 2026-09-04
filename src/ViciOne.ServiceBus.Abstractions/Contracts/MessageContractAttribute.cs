using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Declares a stable application-level identity for a message contract that may cross a durable or wire boundary.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class MessageContractAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="majorVersion">The major version value.</param>
    public MessageContractAttribute(string name, int majorVersion = 1)
    {
        var identity = new MessageContractIdentity(name, majorVersion);
        Name = identity.Name;
        MajorVersion = identity.MajorVersion;
    }

    /// <summary>
    /// Gets the name value.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the major version value.
    /// </summary>
    public int MajorVersion { get; }
}
