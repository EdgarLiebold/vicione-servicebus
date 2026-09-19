namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Specifies a lossless text carrier for the exact admitted envelope bytes.</summary>
public enum SerializedTransportTextFormat
{
    /// <summary>The serializer does not support text-only transports.</summary>
    None = 0,

    /// <summary>The envelope is valid UTF-8 text and is transported without re-encoding.</summary>
    Utf8 = 1,

    /// <summary>The envelope is represented as Base64 text for a binary-safe roundtrip.</summary>
    Base64 = 2,
}
