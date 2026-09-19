using System;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Locates the serialized application body inside a copied transport envelope without re-encoding it.</summary>
internal interface ICopiedEnvelopeBodyExtractor
{
    ReadOnlyMemory<byte> ExtractSerializedBody(ReadOnlyMemory<byte> envelope);
}
