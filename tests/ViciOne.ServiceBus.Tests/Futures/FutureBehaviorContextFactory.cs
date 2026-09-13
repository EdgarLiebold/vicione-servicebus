using ViciOne.ServiceBus.Futures;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;

namespace ViciOne.ServiceBus.Tests.Futures;

internal static class FutureBehaviorContextFactory
{
    public static async Task UseAsync<T>(
        ViciOneServiceBusStateMachine<FutureState> machine,
        IEvent<T> @event,
        FutureState state,
        T message,
        Func<IBehaviorContext<FutureState, T>, Task> callback,
        OutgoingMessageRecorder? outgoingMessages = null,
        CancellationToken cancellationToken = default,
        Uri? responseAddress = null,
        Guid? requestId = null,
        IServiceProvider? serviceProvider = null,
        Guid? messageId = null)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(machine);
        ArgumentNullException.ThrowIfNull(@event);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(callback);

        SerializerContext serializerContext = CreateSerializerContext(message);
        ConsumeContext<T> consumeContext = InMemoryOutboxTestContextFactory.Create(
            message,
            cancellationToken,
            outgoingMessages: outgoingMessages,
            serializerContext: serializerContext,
            responseAddress: responseAddress,
            requestId: requestId,
            serviceProvider: serviceProvider,
            messageId: messageId);
        var sagaInstance = new SagaInstance<FutureState>(state);
        await sagaInstance.MarkInUseAsync(consumeContext.CancellationToken);
        using var sagaContext = new InMemorySagaConsumeContext<FutureState, T>(consumeContext, sagaInstance);
        IBehaviorContext<FutureState, T> behaviorContext =
            new ViciOneServiceBusStateMachine<FutureState>.BehaviorContextProxy<T>(
                machine,
                sagaContext,
                sagaContext,
                @event);

        await callback(behaviorContext);
    }

    private static SerializerContext CreateSerializerContext<T>(T message)
        where T : class
    {
        IObjectDeserializer deserializer = ServiceBusMetadataJson.ObjectDeserializer;
        var metadata = new EnvelopeMessageContext(new JsonMessageEnvelope(), deserializer);
        return new SystemTextJsonSerializerContext(
            deserializer,
            ServiceBusMetadataJson.Options,
            SystemTextJsonMessageSerializer.JsonContentType,
            metadata,
            [MessageUrn.ForTypeString<T>()],
            message: message);
    }
}
