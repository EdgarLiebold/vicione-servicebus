using System;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>
/// Process-local durable-send state carried by the InMemory transport. This object is a pipeline payload only and is
/// deliberately excluded from transport headers and the serialized message body.
/// </summary>
/// <param name="DurableSendId">The durable send id.</param>
/// <param name="ContractIdentity">The contract identity.</param>
/// <param name="Attempt">The attempt.</param>
/// <param name="Metadata">The metadata.</param>
/// <param name="ConsumerCompletion">The consumer completion.</param>
internal sealed record InMemoryDurableSendContext(
    DurableSendId DurableSendId,
    MessageContractIdentity ContractIdentity,
    int Attempt,
    ReadOnlyMemory<byte> Metadata,
    IDurableSendConsumerCompletion ConsumerCompletion);
