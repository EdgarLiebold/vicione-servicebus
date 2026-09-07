using System;

namespace ViciOne.ServiceBus;

/// <summary>Declares the absolute URN used to identify the annotated message contract.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
public sealed class MessageUrnAttribute :
    Attribute
{
    /// <summary>Initializes the attribute with a message-contract URN.</summary>
    /// <param name="urn">The non-empty URN value, without the default <c>urn:message:</c> prefix.</param>
    /// <param name="useDefaultPrefix"><see langword="true"/> to prepend the default prefix; <see langword="false"/> to require an absolute URI.</param>
    public MessageUrnAttribute(string urn, bool useDefaultPrefix = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(urn);

        if (urn.StartsWith(MessageUrn.Prefix, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"Value should not contain the default prefix '{MessageUrn.Prefix}'.", nameof(urn));

        Urn = FormatUrn(urn, useDefaultPrefix);
    }

    /// <summary>Gets the absolute message-contract URN.</summary>
    public Uri Urn { get; }

    static Uri FormatUrn(string urn, bool useDefaultPrefix)
    {
        string fullValue = useDefaultPrefix ? MessageUrn.Prefix + urn : urn;

        if (Uri.TryCreate(fullValue, UriKind.Absolute, out Uri? uri))
            return uri;

        throw new UriFormatException($"Invalid URN: {fullValue}");
    }
}
