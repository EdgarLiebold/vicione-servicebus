using System;

namespace ViciOne.ServiceBus.InMemoryTransport;

/// <summary>
/// Process-local durable-send state carried by the InMemory transport. This object is a pipeline payload only and is
/// deliberately excluded from transport headers and the serialized message body.
/// </summary>
internal sealed record InMemoryDurableSendContext(
    DurableSendId DurableSendId,
    MessageContractIdentity ContractIdentity,
    int Attempt,
    ReadOnlyMemory<byte> Metadata,
    IDurableSendConsumerCompletion ConsumerCompletion);
