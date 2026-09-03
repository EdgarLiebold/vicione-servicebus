namespace ViciOne.ServiceBus;

/// <summary>
/// One claimed durable-send dispatch attempt. <see cref="ConsumerCompletion"/> is an unforgeable process-local capability
/// that a volatile in-process transport may propagate as pipeline context. It must never be serialized onto a wire.
/// </summary>
public readonly record struct DurableSendDispatchContext(
    SerializedDurableSend Message,
    DurableSendId DurableSendId,
    int Attempt,
    IDurableSendConsumerCompletion ConsumerCompletion);
