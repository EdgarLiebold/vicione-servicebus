
namespace ViciOne.ServiceBus.Providers.Persistence;
/// <summary>
/// One claimed durable-send dispatch attempt. <see cref="ConsumerCompletion"/> is an unforgeable process-local capability
/// that a volatile in-process transport may propagate as pipeline context. It must never be serialized onto a wire.
/// </summary>
/// <param name="Message">The message.</param>
/// <param name="DurableSendId">The durable send id.</param>
/// <param name="Attempt">The attempt.</param>
/// <param name="ConsumerCompletion">The consumer completion.</param>
public readonly record struct DurableSendDispatchContext(
    SerializedDurableSend Message,
    DurableSendId DurableSendId,
    int Attempt,
    IDurableSendConsumerCompletion ConsumerCompletion);
