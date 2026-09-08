using System.Runtime.CompilerServices;

namespace ViciOne.ServiceBus.MessagePack.Tests.Serialization;

public sealed class BoundaryPayload
{
    private static readonly ConditionalWeakTable<BoundaryPayload, PayloadHolder> Payloads = [];
    private static int _countSerializationReads;
    private static int _serializationReads;

    public BoundaryPayload()
    {
        Payloads.Add(this, new PayloadHolder([]));
    }

    public BoundaryPayload(byte[] data)
    {
        Payloads.Add(this, new PayloadHolder(data));
    }

    public static int SerializationReads => Volatile.Read(ref _serializationReads);

    public byte[] Data
    {
        get
        {
            if (Volatile.Read(ref _countSerializationReads) != 0)
                Interlocked.Increment(ref _serializationReads);
            return Payloads.GetOrCreateValue(this).Data;
        }
        set => Payloads.GetOrCreateValue(this).Data = value;
    }

    public static void ResetSerializationReads()
    {
        Volatile.Write(ref _serializationReads, 0);
        Volatile.Write(ref _countSerializationReads, 1);
    }

    public static void StopCountingSerializationReads() => Volatile.Write(ref _countSerializationReads, 0);

    public int GetDataLength() => Payloads.GetOrCreateValue(this).Data.Length;

    private sealed class PayloadHolder(byte[] data)
    {
        public byte[] Data { get; set; } = data;
    }
}
