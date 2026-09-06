using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Context;

/// <summary>Carries state for deserializer consume operations.</summary>
public abstract class DeserializerConsumeContext :
    BaseConsumeContext
{
    readonly PendingTaskCollection _consumeTasks;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="receiveContext">The receive context.</param>
    /// <param name="serializerContext">The serializer context.</param>
    protected DeserializerConsumeContext(ReceiveContext receiveContext, SerializerContext serializerContext)
        : base(receiveContext, serializerContext)
    {
        _consumeTasks = new PendingTaskCollection(4);
    }

    /// <summary>Gets the consume completed.</summary>
    public override Task ConsumeCompleted => _consumeTasks.CompletedAsync(CancellationToken);

    /// <summary>Returns true if the payload type is included with or supported by the context type.</summary>
    /// <param name="payloadType">The runtime payload type used by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public override bool HasPayloadType(Type payloadType)
    {
        return payloadType.IsInstanceOfType(this) || ReceiveContext.HasPayloadType(payloadType);
    }

    /// <summary>Attempts to get the specified payload type.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="payload">Receives the payload produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public override bool TryGetPayload<T>([NotNullWhen(true)] out T? payload)
        where T : class
    {
        if (this is T context)
        {
            payload = context;
            return true;
        }

        return ReceiveContext.TryGetPayload(out payload);
    }

    /// <summary>Get or add a payload to the context, using the provided payload factory.</summary>
    /// <typeparam name="T">The payload type.</typeparam>
    /// <param name="payloadFactory">The payload factory, which is only invoked if the payload is not present.</param>
    /// <returns>The or add payload.</returns>
    public override T GetOrAddPayload<T>(PayloadFactory<T> payloadFactory)
    {
        if (this is T context)
            return context;

        return ReceiveContext.GetOrAddPayload(payloadFactory);
    }

    /// <summary>Either adds a new payload, or updates an existing payload.</summary>
    /// <typeparam name="T">The payload type.</typeparam>
    /// <param name="addFactory">The payload factory called if the payload is not present.</param>
    /// <param name="updateFactory">The payload factory called if the payload already exists.</param>
    /// <returns>The t produced by the operation.</returns>
    public override T AddOrUpdatePayload<T>(PayloadFactory<T> addFactory, UpdatePayloadFactory<T> updateFactory)
    {
        if (this is T context)
            return context;

        return ReceiveContext.AddOrUpdatePayload(addFactory, updateFactory);
    }

    /// <summary>Adds consume task to the configuration.</summary>
    /// <param name="task">The task.</param>
    public override void AddConsumeTask(Task task)
    {
        _consumeTasks.Add(task);
    }
}
