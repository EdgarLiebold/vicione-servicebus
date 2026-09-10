using System;

namespace ViciOne.ServiceBus.InMemoryTransport.DurableSend;

/// <summary>
/// Carries process-local durable-send completion state as an in-memory pipeline payload. The state is never written
/// to transport headers or the serialized message body.
/// </summary>
/// <param name="DurableSendId">The durable-send operation identifier.</param>
/// <param name="ContractIdentity">The stable message-contract identity.</param>
/// <param name="Attempt">The one-based delivery attempt.</param>
/// <param name="Metadata">The immutable reliable-envelope metadata.</param>
/// <param name="ConsumerCompletion">The capability used to persist successful consumer completion.</param>
internal sealed record InMemoryDurableSendContext(
    DurableSendId DurableSendId,
    MessageContractIdentity ContractIdentity,
    int Attempt,
    ReadOnlyMemory<byte> Metadata,
    IDurableSendConsumerCompletion ConsumerCompletion);
