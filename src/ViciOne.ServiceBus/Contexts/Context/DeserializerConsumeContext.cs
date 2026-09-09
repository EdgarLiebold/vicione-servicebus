using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Context;

/// <summary>Tracks consume work attached while a transport body is deserialized into message contracts.</summary>
public abstract class DeserializerConsumeContext :
    BaseConsumeContext
{
    readonly PendingTaskCollection _consumeTasks;

    /// <summary>Creates a deserialization-backed consume context.</summary>
    /// <param name="receiveContext">The transport receive context that owns delivery completion.</param>
    /// <param name="serializerContext">The serializer context used to materialize messages.</param>
    protected DeserializerConsumeContext(ReceiveContext receiveContext, SerializerContext serializerContext)
        : base(receiveContext, serializerContext)
    {
        _consumeTasks = new PendingTaskCollection(4);
    }

    /// <inheritdoc />
    public override Task ConsumeCompleted => _consumeTasks.CompletedAsync(CancellationToken);

    /// <inheritdoc />
    public override bool HasPayloadType(Type payloadType)
    {
        ArgumentNullException.ThrowIfNull(payloadType);
        return payloadType.IsInstanceOfType(this) || ReceiveContext.HasPayloadType(payloadType);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public override T GetOrAddPayload<T>(PayloadFactory<T> payloadFactory)
    {
        ArgumentNullException.ThrowIfNull(payloadFactory);

        if (this is T context)
            return context;

        return ReceiveContext.GetOrAddPayload(payloadFactory);
    }

    /// <inheritdoc />
    public override T AddOrUpdatePayload<T>(PayloadFactory<T> addFactory, UpdatePayloadFactory<T> updateFactory)
    {
        ArgumentNullException.ThrowIfNull(addFactory);
        ArgumentNullException.ThrowIfNull(updateFactory);

        if (this is T context)
            return context;

        return ReceiveContext.AddOrUpdatePayload(addFactory, updateFactory);
    }

    /// <inheritdoc />
    public override void AddConsumeTask(Task task)
    {
        ArgumentNullException.ThrowIfNull(task);
        _consumeTasks.Add(task);
    }
}
