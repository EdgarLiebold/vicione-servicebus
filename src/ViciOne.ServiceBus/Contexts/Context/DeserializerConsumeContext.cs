using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Context;

/// <summary>
/// Provides a deserializer consume context implementation.
/// </summary>
public abstract class DeserializerConsumeContext :
    BaseConsumeContext
{
    readonly PendingTaskCollection _consumeTasks;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="receiveContext">The receive context value.</param>
    /// <param name="serializerContext">The serializer context value.</param>
    protected DeserializerConsumeContext(ReceiveContext receiveContext, SerializerContext serializerContext)
        : base(receiveContext, serializerContext)
    {
        _consumeTasks = new PendingTaskCollection(4);
    }

    /// <summary>
    /// Gets the consume completed value.
    /// </summary>
    public override Task ConsumeCompleted => _consumeTasks.CompletedAsync(CancellationToken);

    /// <summary>
    /// Returns true if the payload type is included with or supported by the context type
    /// </summary>
    /// <param name="payloadType"></param>
    /// <returns></returns>
    public override bool HasPayloadType(Type payloadType)
    {
        return payloadType.IsInstanceOfType(this) || ReceiveContext.HasPayloadType(payloadType);
    }

    /// <summary>
    /// Attempts to get the specified payload type
    /// </summary>
    /// <param name="payload"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
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

    /// <summary>
    /// Get or add a payload to the context, using the provided payload factory.
    /// </summary>
    /// <param name="payloadFactory">The payload factory, which is only invoked if the payload is not present.</param>
    /// <typeparam name="T">The payload type</typeparam>
    /// <returns></returns>
    public override T GetOrAddPayload<T>(PayloadFactory<T> payloadFactory)
    {
        if (this is T context)
            return context;

        return ReceiveContext.GetOrAddPayload(payloadFactory);
    }

    /// <summary>
    /// Either adds a new payload, or updates an existing payload
    /// </summary>
    /// <param name="addFactory">The payload factory called if the payload is not present</param>
    /// <param name="updateFactory">The payload factory called if the payload already exists</param>
    /// <typeparam name="T">The payload type</typeparam>
    /// <returns></returns>
    public override T AddOrUpdatePayload<T>(PayloadFactory<T> addFactory, UpdatePayloadFactory<T> updateFactory)
    {
        if (this is T context)
            return context;

        return ReceiveContext.AddOrUpdatePayload(addFactory, updateFactory);
    }

    /// <summary>
    /// Adds consume task to the configuration.
    /// </summary>
    /// <param name="task">The task value.</param>
    public override void AddConsumeTask(Task task)
    {
        _consumeTasks.Add(task);
    }
}
